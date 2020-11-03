using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Service.App_LocalResources;
using Lithnet.AccessManager.Service.Internal;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Service.AppSettings
{
    public class CertificateAuthenticationProvider : HttpContextAuthenticationProvider, ICertificateAuthenticationProvider
    {
        private static readonly Oid smartCardOid = new Oid("1.3.6.1.4.1.311.20.2.2");

        private readonly CertificateAuthenticationProviderOptions options;

        private readonly IDirectory directory;

        private readonly ILogger<CertificateAuthenticationProvider> logger;

        private readonly IMemoryCache cache;

        public CertificateAuthenticationProvider(IOptionsSnapshot<CertificateAuthenticationProviderOptions> options, ILogger<CertificateAuthenticationProvider> logger, IDirectory directory, IHttpContextAccessor httpContextAccessor, IAuthorizationContextProvider authzContextProvider)
            : base(httpContextAccessor, directory, authzContextProvider)
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
                        if (this.cache.TryGetValue(context.ClientCertificate.Thumbprint, out ClaimsPrincipal identity))
                        {
                            context.Principal = identity;
                            context.Success();
                            return Task.CompletedTask;
                        }

                        this.logger.LogTrace("Certificate passed basic validations");

                        var claims = (context.Principal?.Identity as ClaimsIdentity)?.ToClaimList();

                        try
                        {
                            this.ValidateCertificate(context);
                            this.logger.LogTrace("Certificate passed all required validation checks");
                        }
                        catch (Exception ex)
                        {
                            context.Fail(ex);
                            context.Principal = null;
                            this.logger.LogError(EventIDs.CertificateValidationError, ex, $"The certificate could not be validated. {ex.Message}\r\n\r\n{claims}\r\n{context.HttpContext.Connection.ClientCertificate}");
                            return Task.CompletedTask;
                        }

                        try
                        {
                            this.ResolveIdentity(context);
                            this.logger.LogTrace("Identity successfully resolved");
                        }
                        catch (Exception ex)
                        {
                            context.Fail(ex);
                            context.Principal = null;
                            this.logger.LogError(EventIDs.CertificateIdentityNotFound, ex, $"The identity represented by the certificate could not be resolved. {ex.Message}\r\n\r\n{claims}\r\n{context.HttpContext.Connection.ClientCertificate}");
                            return Task.CompletedTask;
                        }

                        var updatedClaims = (context.Principal?.Identity as ClaimsIdentity)?.ToClaimList();

                        this.logger.LogInformation(EventIDs.UserAuthenticated, string.Format(LogMessages.AuthenticatedAndMappedUser, updatedClaims));
                        context.Success();

                        this.cache.Set(context.ClientCertificate.Thumbprint, context.Principal, TimeSpan.FromMinutes(30));
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        this.logger.LogError(EventIDs.CertificateAuthNError, context.Exception, string.Format(LogMessages.CertificateAuthNGenericFailure, context.HttpContext.Connection.ClientCertificate));
                        context.Response.Redirect($"/Home/AuthNError?messageID={(int)AuthNFailureMessageID.InvalidCertificate}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        this.logger.LogError(EventIDs.CertificateAuthNAccessDenied, string.Format(LogMessages.CertificateAuthNValidationFailure, context.HttpContext.Connection.ClientCertificate));
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

            this.logger.LogTrace("Attempting to resolve certificate identity. Resolution mode: {mode}", this.options.IdentityResolutionMode);

            if ((this.options.IdentityResolutionMode == CertificateIdentityResolutionMode.Default ||
                this.options.IdentityResolutionMode.HasFlag(CertificateIdentityResolutionMode.UpnSan)))
            {
                this.logger.LogTrace("Attempting identity resolution using UPN");
                string upn = user?.FindFirst(ClaimTypes.Upn)?.Value;

                if (upn == null)
                {
                    string warning = $"The certificate for subject '{context.ClientCertificate.Subject}' did not contain an subject alternative name containing the user's UPN";
                    if (!this.options.IdentityResolutionMode.HasFlag(CertificateIdentityResolutionMode.AltSecurityIdentities))
                    {
                        throw new CertificateIdentityNotFoundException(warning);
                    }
                    else
                    {
                        this.logger.LogWarning(warning);
                    }
                }
                else
                {
                    this.logger.LogTrace("UPN value found {upn}", upn);

                    if (this.TryResolveIdentityUpnSan(context, user, upn))
                    {
                        return;
                    }
                }
            }

            if (this.options.IdentityResolutionMode.HasFlag(CertificateIdentityResolutionMode.AltSecurityIdentities))
            {
                this.logger.LogTrace("Attempting identity resolution using altSecurityIdentities");

                if (this.TryResolveIdentityAltSecurityIdentities(context, user))
                {
                    return;
                }
            }

            string message = string.Format(LogMessages.UserNotFoundInDirectory, user.ToClaimList());
            throw new CertificateIdentityNotFoundException(message);
        }

        private bool TryResolveIdentityUpnSan(CertificateValidatedContext context, ClaimsIdentity user, string upn)
        {
            if (this.directory.TryGetUser(upn, out IUser u))
            {
                this.logger.LogTrace("Found user {user} with upn {upn}", u.MsDsPrincipalName, upn);
                user.AddClaim(new Claim(ClaimTypes.PrimarySid, u.Sid.ToString(), context.Options.ClaimsIssuer));
                this.AddAuthZClaims(u, user);
                return true;
            }

            return false;
        }

        private bool TryResolveIdentityAltSecurityIdentities(CertificateValidatedContext context, ClaimsIdentity user)
        {
            string subject = context.ClientCertificate.SubjectName.ToCommaSeparatedString();
            string issuer = context.ClientCertificate.IssuerName.ToCommaSeparatedString();
            string serialNumber = context.ClientCertificate.GetSerialNumber().ToHexString();
            string ski = context.ClientCertificate.Extensions.OfType<X509SubjectKeyIdentifierExtension>().FirstOrDefault()?.SubjectKeyIdentifier;
            string sha1keyhash = SHA1.Create().ComputeHash(context.ClientCertificate.GetPublicKey()).ToHexString();
            string rfc822 = context.ClientCertificate.GetNameInfo(X509NameType.EmailName, false);

            List<string> altSecurityIdentities = new List<string>();

            if (!string.IsNullOrWhiteSpace(issuer) && !string.IsNullOrWhiteSpace(subject))
            {
                altSecurityIdentities.Add($"X509:<I>{issuer}<S>{subject}");
            }

            if (!string.IsNullOrWhiteSpace(subject) && this.options.ValidationMethod == ClientCertificateValidationMethod.NtAuthStore)
            {
                altSecurityIdentities.Add($"X509:<S>{subject}");
            }

            if (!string.IsNullOrWhiteSpace(issuer) && !string.IsNullOrWhiteSpace(serialNumber))
            {
                altSecurityIdentities.Add($"X509:<I>{issuer}<SR>{serialNumber}");
            }

            if (!string.IsNullOrWhiteSpace(ski))
            {
                altSecurityIdentities.Add($"X509:<SKI>{ski}");
            }

            if (!string.IsNullOrWhiteSpace(sha1keyhash))
            {
                altSecurityIdentities.Add($"X509:<SHA1-PUKEY>{sha1keyhash}");
            }

            if (!string.IsNullOrWhiteSpace(rfc822) && this.options.ValidationMethod == ClientCertificateValidationMethod.NtAuthStore)
            {
                altSecurityIdentities.Add($"X509:<RFC822>{rfc822}");
            }

            foreach (string altSecurityIdentity in altSecurityIdentities)
            {
                this.logger.LogTrace("Attempting to find user with altSecurityIdentity {altSecurityIdentity}", altSecurityIdentity);

                if (this.directory.TryGetUserByAltSecurityIdentity(altSecurityIdentity, out IUser u))
                {
                    this.logger.LogTrace("Found user {user} with altSecurityIdentity {altSecurityIdentity}", u.MsDsPrincipalName, altSecurityIdentity);
                    user.AddClaim(new Claim(ClaimTypes.PrimarySid, u.Sid.ToString(), context.Options.ClaimsIssuer));
                    this.AddAuthZClaims(u, user);
                    return true;
                }
            }

            return false;
        }

        private bool ValidateNtAuthStore(X509Chain chain)
        {
            if (this.options.ValidationMethod != ClientCertificateValidationMethod.NtAuthStore)
            {
                return true;
            }

            this.logger.LogTrace("Attempting to validate certificate against the NTAuth store");

            Crypt32.CERT_CHAIN_POLICY_STATUS status = new Crypt32.CERT_CHAIN_POLICY_STATUS();
            var para = new Crypt32.CERT_CHAIN_POLICY_PARA
            {
                cbSize = (uint)Marshal.SizeOf<Crypt32.CERT_CHAIN_POLICY_PARA>()
            };

            if (!Crypt32.CertVerifyCertificateChainPolicy(6, chain.ChainContext, para, ref status))
            {
                throw new CertificateValidationException("The function used to validate the certificate chain failed", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            if (status.dwError != 0)
            {
                throw new CertificateValidationException("The certificate could not be validated against the NTAuth store. Ensure the issuer is from a trusted enterprise smart-card issuing CA", new Win32Exception((int)status.dwError));
            }

            this.logger.LogTrace("Certificate successfully validated against the NTAuth store");
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
            
            this.logger.LogTrace("Attempting to validate certificate against the list of trusted issuers");

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
                            this.logger.LogTrace("Certificate validated against specific issuer {issuer}", issuer.Subject);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.CertificateTrustChainParsingIssue, ex, $"Unable to parse trusted issuer certificate at index {count}");
                }
            }

            this.logger.LogTrace("Certificate could not be validated against a specific issuer");

            return false;
        }
    }
}