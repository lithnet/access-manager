using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Auditing;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Extensions;
using Lithnet.AccessManager.Web.App_LocalResources;
using Lithnet.AccessManager.Web.Internal;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public class CertificateAuthenticationProvider : HttpContextAuthenticationProvider, ICertificateAuthenticationProvider
    {
        private static readonly Oid smartCardOid = new Oid("1.3.6.1.4.1.311.20.2.2");

        private readonly CertificateAuthenticationProviderOptions options;

        private readonly IDirectory directory;

        private readonly ILogger<CertificateAuthenticationProvider> logger;

        private readonly IMemoryCache cache;

        public CertificateAuthenticationProvider(IOptionsSnapshot<CertificateAuthenticationProviderOptions> options, ILogger<CertificateAuthenticationProvider> logger, IDirectory directory, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor, directory)
        {
            this.directory = directory;
            this.logger = logger;
            this.cache = new MemoryCache(new MemoryCacheOptions
            {
                 
            });

            this.options = options.Value;
        }

        public override bool CanLogout => false;

        public override bool IdpLogout => false;

        public override void Configure(IServiceCollection services)
        {
            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate(o =>
            {
                o.AllowedCertificateTypes = CertificateTypes.Chained;
                o.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                o.RevocationMode = X509RevocationMode.Online;
                o.ValidateCertificateUse = true;
                o.ValidateValidityPeriod = true;

                o.Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        if(this.cache.TryGetValue(context.ClientCertificate.Thumbprint, out ClaimsPrincipal identity))
                        {
                            context.Principal = identity;
                            context.Success();
                            return Task.CompletedTask;
                        }

                        var claims = (context.Principal?.Identity as ClaimsIdentity)?.ToClaimList();

                        try
                        {
                            this.ValidateCertificate(context);
                        }
                        catch (Exception ex)
                        {
                            context.Fail(ex);
                            context.Principal = null;
                            this.logger.LogEventError(EventIDs.CertificateValidationError, $"The certificate could not be validated. {ex.Message}\r\n\r\n{claims}\r\n{context.HttpContext.Connection.ClientCertificate}", ex);
                            return Task.CompletedTask;
                        }

                        try
                        {
                            this.ResolveIdentity(context);
                        }
                        catch (Exception ex)
                        {
                            context.Fail(ex);
                            context.Principal = null;
                            this.logger.LogEventError(EventIDs.CertificateIdentityNotFound, $"The identity represented by the certificate could not be resolved. {ex.Message}\r\n\r\n{claims}\r\n{context.HttpContext.Connection.ClientCertificate}", ex);
                            return Task.CompletedTask;
                        }

                        var updatedClaims = (context.Principal?.Identity as ClaimsIdentity)?.ToClaimList();

                        this.logger.LogEventSuccess(EventIDs.UserAuthenticated, string.Format(LogMessages.AuthenticatedAndMappedUser, updatedClaims));
                        context.Success();

                        this.cache.Set(context.ClientCertificate.Thumbprint, context.Principal, TimeSpan.FromMinutes(30));
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        this.logger.LogEventError(EventIDs.CertificateAuthNError, string.Format(LogMessages.CertificateAuthNGenericFailure, context.HttpContext.Connection.ClientCertificate), context.Exception);
                        context.Response.Redirect($"/Home/AuthNError?messageID={(int)AuthNFailureMessageID.InvalidCertificate}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    { 
                        this.logger.LogEventError(EventIDs.CertificateAuthNAccessDenied, string.Format(LogMessages.CertificateAuthNValidationFailure, context.HttpContext.Connection.ClientCertificate));
                        context.HandleResponse();
                        context.Response.Redirect($"/Home/AuthNError?messageID={(int)AuthNFailureMessageID.InvalidCertificate}");
                        return Task.CompletedTask;
                    }
                };
            });
        }

        private void ValidateCertificate(CertificateValidatedContext context)
        {
            X509Chain chain = new X509Chain
            {
                ChainPolicy = this.BuildChainPolicy()
            };

            if (!chain.Build(context.ClientCertificate))
            {
                throw new CertificateValidationException("The certificate did not contain the required EKUs");
            }

            if (!this.ValidateIssuer(chain))
            {
                throw new CertificateValidationException("One of the required issuers was not found in the certificate chain");
            }

            if (!this.ValidateNtAuthStore(chain))
            {
                throw new CertificateValidationException("The certificate chain did not validate to an issuer that is trusted by the enterprise to issue smart card certificates");
            }
        }

        private void ResolveIdentity(CertificateValidatedContext context)
        {
            ClaimsIdentity user = context.Principal.Identity as ClaimsIdentity;
            Claim c = user?.FindFirst(ClaimTypes.Upn);

            if (c?.Value == null)
            {
                string message = $"The certificate for subject '{context.ClientCertificate.Subject}' did not contain an alternate subject name containing the user's UPN";
                throw new CertificateIdentityNotFoundException(message);
            }

            if (!this.directory.TryGetUser(c.Value, out IUser u))
            {
                string message = string.Format(LogMessages.UserNotFoundInDirectory, user.ToClaimList());
                throw new CertificateIdentityNotFoundException(message);
            }

            user.AddClaim(new Claim(ClaimTypes.PrimarySid, u.Sid.ToString(), context.Options.ClaimsIssuer));
        }

        private bool ValidateNtAuthStore(X509Chain chain)
        {
            if (this.options.ValidationMethod != ClientCertificateValidationMethod.NtAuthStore)
            {
                return true;
            }

            Crypt32.CERT_CHAIN_POLICY_STATUS status = new Crypt32.CERT_CHAIN_POLICY_STATUS();
            var para = new Crypt32.CERT_CHAIN_POLICY_PARA
            {
                cbSize = (uint)Marshal.SizeOf<Crypt32.CERT_CHAIN_POLICY_PARA>()
            };

            if (!Crypt32.CertVerifyCertificateChainPolicy(6, chain.ChainContext, para, ref status))
            {
                throw new CertificateValidationException("The function used to validated the certificate chain failed", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            if (status.dwError != 0)
            {
                throw new CertificateValidationException("The certificate could not be validated against the NTAuth store. Ensure the issuer is from a trusted enterprise smart-card issuing CA", new Win32Exception((int)status.dwError));
            }

            return true;
        }

        private X509ChainPolicy BuildChainPolicy()
        {
            var chainPolicy = new X509ChainPolicy();

            if (this.options.RequireSmartCardLogonEku)
            {
                chainPolicy.ApplicationPolicy.Add(smartCardOid);
            }

            if (this.options.RequiredEkus?.Count > 0)
            {
                foreach (var eku in this.options.RequiredEkus)
                {
                    chainPolicy.ApplicationPolicy.Add(new Oid(eku));
                }
            }

            return chainPolicy;
        }

        private bool ValidateIssuer(X509Chain chain)
        {
            if (this.options.ValidationMethod != ClientCertificateValidationMethod.SpecificIssuer)
            {
                return true;
            }

            if (this.options.TrustedIssuers == null || this.options.TrustedIssuers.Count == 0)
            {
                return false;
            }

            int count = 0;

            foreach (var issuerEncoded in this.options.TrustedIssuers)
            {
                count++;

                try
                {
                    X509Certificate2 issuer = new X509Certificate2(Convert.FromBase64String(issuerEncoded));

                    foreach (var item in chain.ChainElements)
                    {
                        if (string.Equals(item.Certificate.Thumbprint, issuer.Thumbprint, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Unable to parse trusted issuer certificate at index {count}");
                }
            }

            return false;
        }
    }
}