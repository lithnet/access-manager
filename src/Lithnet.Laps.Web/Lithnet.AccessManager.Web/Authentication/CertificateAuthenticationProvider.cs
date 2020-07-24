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
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NLog;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public class CertificateAuthenticationProvider : HttpContextAuthenticationProvider, ICertificateAuthenticationProvider
    {
        private readonly CertificateAuthenticationProviderOptions options;

        private readonly IDirectory directory;

        private readonly ILogger logger;

        public CertificateAuthenticationProvider(IOptions<CertificateAuthenticationProviderOptions> options, ILogger logger, IDirectory directory, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor, directory)
        {
            this.directory = directory;
            this.logger = logger;
            this.options = options.Value;

        }

        public override bool CanLogout => false;

        public override bool IdpLogout => false;

        public override void Configure(IServiceCollection services)
        {
            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate(o =>
            {
                o.AllowedCertificateTypes = CertificateTypes.Chained;
                o.RevocationFlag = this.options.RevocationFlag;
                o.RevocationMode = this.options.RevocationMode;
                o.ValidateCertificateUse = true;
                o.ValidateValidityPeriod = true;

                o.Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier,  context.ClientCertificate.Subject, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                            new Claim(ClaimTypes.Name, context.ClientCertificate.Subject,  ClaimValueTypes.String,  context.Options.ClaimsIssuer),
                        };

                        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));

                        if (!ValidateCertificate(context.ClientCertificate))
                        {
                            context.Fail("Additional certificate validation failed");
                            return Task.CompletedTask;
                        }

                        if (!ResolveIdentity(context))
                        {
                            context.Fail("Could not resolve the certificate identity");
                            return Task.CompletedTask;
                        }

                        context.Success();
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.SsoIdentityNotFound}");
                        return Task.CompletedTask;
                    }
                };
            });
        }

        private bool ResolveIdentity(CertificateValidatedContext context)
        {
            try
            {
                ClaimsIdentity user = (ClaimsIdentity)context.Principal.Identity;

                string upn = context.ClientCertificate.GetNameInfo(X509NameType.UpnName, false);

                if (upn != null)
                {
                    user.AddClaim(new Claim(ClaimTypes.Upn, upn));
                }
                else
                {
                    this.logger.LogEventError(EventIDs.CertificateMissingUpn, $"The certificate for subject '{context.ClientCertificate.Subject}' did not contain an alternate subject name containing the user's UPN");
                    return false;
                }

                if (!this.directory.TryGetUser(upn, out IUser u))
                {
                    string message = string.Format(LogMessages.UserNotFoundInDirectory, user.ToClaimList());
                    this.logger.LogEventError(EventIDs.SsoIdentityNotFound, message, null);
                    return false;
                }

                user.AddClaim(new Claim(ClaimTypes.PrimarySid, u.Sid.ToString()));
                this.logger.LogEventSuccess(EventIDs.UserAuthenticated, string.Format(LogMessages.AuthenticatedAndMappedUser, user.ToClaimList()));
                context.Success();
                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogEventError(EventIDs.AuthNResponseProcessingError, LogMessages.AuthNResponseProcessingError, ex);
                context.Fail("Processing error");
                return false;
            }
        }

        private bool ValidateCertificate(X509Certificate2 clientCertificate)
        {
            X509Chain chain = new X509Chain();

            if (!string.IsNullOrWhiteSpace(this.options.RequireCustomEku))
            {
                chain.ChainPolicy.ApplicationPolicy.Add(new Oid(this.options.RequireCustomEku));
            }

            if (this.options.RequireSmartCardLogonEku)
            {
                chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.4.1.311.20.2.2"));
            }

            if (!chain.Build(clientCertificate))
            {
                logger.Error("Unable to validate chain. Ensure that the certificate is trusted and that the required EKUs are present");
                return false;
            }

            if (this.options.IssuerThumbprints?.Count > 0)
            {
                bool matched = false;

                foreach (var issuer in this.options.IssuerThumbprints)
                {
                    foreach (var item in chain.ChainElements)
                    {
                        if (string.Equals(item.Certificate.Thumbprint, issuer, StringComparison.OrdinalIgnoreCase))
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (matched)
                    {
                        break;
                    }
                }

                if (!matched)
                {
                    logger.Error("Unable to validate chain to a mandated issuer");
                    return false;
                }
            }

            if (this.options.MustValidateToNTAuth)
            {
                Crypt32.CERT_CHAIN_POLICY_STATUS status = new Crypt32.CERT_CHAIN_POLICY_STATUS();
                var para = new Crypt32.CERT_CHAIN_POLICY_PARA
                {
                    cbSize = (uint)Marshal.SizeOf<Crypt32.CERT_CHAIN_POLICY_PARA>()
                };

                if (!Crypt32.CertVerifyCertificateChainPolicy(6, chain.ChainContext, para, ref status))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (status.dwError == 0)
                {
                    return true;
                }

                this.logger.Error(new Win32Exception((int)status.dwError), "The certificate could not be validated against the NTAuth store");
                return false;
            }

            return true;
        }
    }
}