﻿using System;
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

        private readonly ILogger logger;

        public CertificateAuthenticationProvider(IOptions<CertificateAuthenticationProviderOptions> options, ILogger<CertificateAuthenticationProvider> logger, IDirectory directory, IHttpContextAccessor httpContextAccessor)
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
                o.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                o.RevocationMode = X509RevocationMode.Online;
                o.ValidateCertificateUse = true;
                o.ValidateValidityPeriod = true;
                
                o.Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        if (!ValidateCertificate(context))
                        {
                            context.Principal = null;
                            return Task.CompletedTask;
                        }

                        if (!ResolveIdentity(context))
                        {
                            context.Principal = null;
                            return Task.CompletedTask;
                        }

                        context.Success();
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        this.logger.LogEventError(EventIDs.CertificateAuthNError, LogMessages.AuthNAccessDenied, context.Exception);
                        context.Response.Redirect($"/Home/AuthNError?messageID={(int)AuthNFailureMessageID.InvalidCertificate}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        this.logger.LogEventError(EventIDs.CertificateAuthNAccessDenied, LogMessages.AuthNAccessDenied);
                        context.HandleResponse();
                        context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.InvalidCertificate}");
                        return Task.CompletedTask;
                    }
                };
            });
        }

        private bool ValidateCertificate(CertificateValidatedContext context)
        {
            X509Chain chain = new X509Chain
            {
                ChainPolicy = this.BuildChainPolicy()
            };

            if (!chain.Build(context.ClientCertificate))
            {
                context.Fail(new CertificateValidationException("The certificate did not contain the required EKUs"));
                return false;
            }

            if (!this.ValidateIssuer(chain))
            {
                context.Fail(new CertificateValidationException("One of the required issuers was not found in the certificate chain"));
                return false;
            }

            try
            {
                if (!this.ValidateNtAuthStore(chain))
                {
                    context.Fail(new CertificateValidationException("The certificate chain did not validate to an issuer that is trusted by the enterprise to issue smart card certificates"));
                    return false;
                }
            }
            catch (CertificateValidationException ex)
            {
                context.Fail(ex);
                return false;
            }

            return true;
        }

        private bool ResolveIdentity(CertificateValidatedContext context)
        {
            try
            {
                ClaimsIdentity user = context.Principal.Identity as ClaimsIdentity;
                Claim c = user?.FindFirst(ClaimTypes.Upn);

                if (c?.Value == null)
                {
                    this.logger.LogEventError(EventIDs.CertificateMissingUpn, $"The certificate for subject '{context.ClientCertificate.Subject}' did not contain an alternate subject name containing the user's UPN");
                    context.Fail("The subject's UPN was not found");
                    return false;
                }

                if (!this.directory.TryGetUser(c.Value, out IUser u))
                {
                    string message = string.Format(LogMessages.UserNotFoundInDirectory, user.ToClaimList());
                    this.logger.LogEventError(EventIDs.CertificateIdentityNotFound, message, null);
                    context.Fail("The user was not found in the directory");
                    return false;
                }

                user.AddClaim(new Claim(ClaimTypes.PrimarySid, u.Sid.ToString(), context.Options.ClaimsIssuer));
                this.logger.LogEventSuccess(EventIDs.UserAuthenticated, string.Format(LogMessages.AuthenticatedAndMappedUser, user.ToClaimList()));
                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogEventError(EventIDs.AuthNResponseProcessingError, LogMessages.AuthNResponseProcessingError, ex);
                context.Fail("Error resolving identity from certificate");
                return false;
            }
        }


        private bool ValidateNtAuthStore(X509Chain chain)
        {
            if (!this.options.MustValidateToNTAuth)
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
                throw new CertificateValidationException("The certificate could not be validated against the NTAuth store. Ensure the issuer is from a trusted enterprise smart-card issuing CA", new Win32Exception((int) status.dwError));
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

            if (!string.IsNullOrWhiteSpace(this.options.RequiredCustomEku))
            {
                chainPolicy.ApplicationPolicy.Add(new Oid(this.options.RequiredCustomEku));
            }

            return chainPolicy;
        }

        private bool ValidateIssuer(X509Chain chain)
        {
            if (this.options.IssuerThumbprints == null || this.options.IssuerThumbprints.Count == 0)
            {
                return true;
            }

            foreach (var issuer in this.options.IssuerThumbprints)
            {
                foreach (var item in chain.ChainElements)
                {
                    if (string.Equals(item.Certificate.Thumbprint, issuer, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

            }

            return false;
        }
    }
}