using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Lithnet.AccessManager.Api;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class ApplicationConfig : IApplicationConfig
    {
        [JsonIgnore]
        public string Path { get; private set; }

        [JsonIgnore]
        public string Hash { get; private set; }

        public ConfigurationMetadata Metadata { get; set; } = new ConfigurationMetadata();

        public LicensingOptions Licensing { get; set; } = new LicensingOptions();

        public AuthenticationOptions Authentication { get; set; } = new AuthenticationOptions();

        public AzureAdOptions AzureAd { get; set; } = new AzureAdOptions();

        public AuditOptions Auditing { get; set; } = new AuditOptions();

        public EmailOptions Email { get; set; } = new EmailOptions();

        public AdminNotificationOptions AdminNotifications { get; set; } = new AdminNotificationOptions();

        public RateLimitOptions RateLimits { get; set; } = new RateLimitOptions();

        public UserInterfaceOptions UserInterface { get; set; } = new UserInterfaceOptions();

        public ForwardedHeadersAppOptions ForwardedHeaders { get; set; } = new ForwardedHeadersAppOptions();

        public DataProtectionOptions DataProtection { get; set; } = new DataProtectionOptions();

        public JitConfigurationOptions JitConfiguration { get; set; } = new JitConfigurationOptions();

        public TokenIssuerOptions TokenIssuer { get; set; } = new TokenIssuerOptions();

        public PasswordPolicyOptions PasswordPolicy { get; set; } = new PasswordPolicyOptions();

        public ApiAuthenticationOptions ApiAuthentication { get; set; } = new ApiAuthenticationOptions();

        public DatabaseOptions Database { get; set; } = new DatabaseOptions();
        
        [JsonExtensionData]
        public IDictionary<string, object> OtherData { get; set; }

        public AuthorizationOptions Authorization { get; set; } = new AuthorizationOptions();

        public void Save(string file, bool forceOverwrite)
        {
            if (!forceOverwrite)
            {
                if (this.HasFileBeenModified())
                {
                    throw new ConfigurationModifiedException("The configuration file has been modified outside of this editor session");
                }
            }

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            this.Metadata.Usn++;

            string data = JsonConvert.SerializeObject(this, settings);
            File.WriteAllText(file, data);
            this.Hash = ApplicationConfig.BuildHash(file);
        }

        public bool HasFileBeenModified()
        {
            var existingFile = ApplicationConfig.Load(this.Path);
            return existingFile.Hash != this.Hash;
        }

        public static IApplicationConfig Load(string file)
        {
            string data = File.ReadAllText(file);
            var result = JsonConvert.DeserializeObject<ApplicationConfig>(data);
            result.Path = file;
            result.Metadata.ValidateMetadata();
            result.Hash = ApplicationConfig.BuildHash(file);

            return result;
        }

        private static string BuildHash(string file)
        {
            using (var hash = SHA1.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    return hash.ComputeHash(stream).ToHexString();
                }
            }
        }
    }
}
