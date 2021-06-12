using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Api.Controllers
{
    [ApiController]
    [Route("agent/metadata")]
    [Produces("application/json")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true)]
    public class MetadataController : Controller
    {
        private readonly IOptions<AgentOptions> agentOptions;
        private readonly ICertificateProvider certificateProvider;
        private readonly ILogger<MetadataController> logger;
        private readonly IApiErrorResponseProvider errorProvider;
        private string certData;
        private DateTime certificateLastAccessed;

        public MetadataController(IOptions<AgentOptions> agentOptions, ICertificateProvider certificateProvider, ILogger<MetadataController> logger, IApiErrorResponseProvider errorProvider)
        {
            this.agentOptions = agentOptions;
            this.certificateProvider = certificateProvider;
            this.logger = logger;
            this.errorProvider = errorProvider;
        }

        public IActionResult Index()
        {
            try
            {
                List<string> allowedOptions = new List<string>();

                if (this.agentOptions.Value.AllowAadAuth)
                {
                    if (this.agentOptions.Value.AllowAzureAdJoinedDevices)
                    {
                        allowedOptions.Add("aadj");
                    }

                    if (this.agentOptions.Value.AllowAzureAdRegisteredDevices)
                    {
                        allowedOptions.Add("aadr");
                    }
                }

                if (this.agentOptions.Value.AllowWindowsAuth)
                {
                    allowedOptions.Add("iwa");
                }

                if (this.agentOptions.Value.AllowSelfSignedAuth)
                {
                    allowedOptions.Add("ssa");
                }

                return this.Json(new MetadataResponse
                {
                    AgentAuthentication = new AgentAuthentication { AllowedOptions = allowedOptions },
                    PasswordManagement = new PasswordManagement { EncryptionCertificate = this.GetCertificateString() }
                });

            }
            catch (Exception ex)
            {
                return this.errorProvider.GetErrorResult(ex);
            }
        }

        private string GetCertificateString()
        {
            if (this.certData == null || (this.certificateLastAccessed.AddMinutes(this.agentOptions.Value.EncryptionCertificateCacheDurationMinutes) < DateTime.UtcNow))
            {
                try
                {
                    X509Certificate2 cert = this.certificateProvider.FindEncryptionCertificate();
                    if (cert != null)
                    {
                        certData = Convert.ToBase64String(cert.Export(X509ContentType.Cert));
                        certificateLastAccessed = DateTime.UtcNow;
                    }
                }
                catch (CertificateNotFoundException)
                {
                    this.logger.LogWarning("The encryption certificate requested by the metadata agent could not be found");
                }
            }

            return certData;
        }
    }
}
