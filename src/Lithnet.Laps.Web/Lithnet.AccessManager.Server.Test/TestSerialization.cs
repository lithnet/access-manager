using System;
using System.Collections.Generic;
using System.Linq;
using Lithnet.AccessManager.Server.Configuration;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Lithnet.AccessManager.Server.Test
{
    public class TestSerialization
    {
        [Test]
        public void SerializeAuditNotificationChannels()
        {
            AuditNotificationChannels channels = new AuditNotificationChannels();
            channels.OnFailure = new List<string>() { "1" };
            channels.OnSuccess = new List<string>() { "2" };

            AuditNotificationChannels newChannels = JsonConvert.DeserializeObject<AuditNotificationChannels>(JsonConvert.SerializeObject(channels));

            CollectionAssert.AreEqual(channels.OnFailure, newChannels.OnFailure);
        }

        [Test]
        public void SerializeWsFedAuthenticationProviderOptions()
        {
            WsFedAuthenticationProviderOptions s = new WsFedAuthenticationProviderOptions();
            s.ClaimName = TestContext.CurrentContext.Random.GetString();
            s.IdpLogout = true;
            s.Metadata = TestContext.CurrentContext.Random.GetString();
            s.Realm = TestContext.CurrentContext.Random.GetString();

            WsFedAuthenticationProviderOptions n = JsonConvert.DeserializeObject<WsFedAuthenticationProviderOptions>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.ClaimName, n.ClaimName);
            Assert.AreEqual(s.IdpLogout, n.IdpLogout);
            Assert.AreEqual(s.Metadata, n.Metadata);
            Assert.AreEqual(s.Realm, n.Realm);
        }

        [Test]
        public void SerializeWebhookNotificationChannelDefinition()
        {
            WebhookNotificationChannelDefinition s = new WebhookNotificationChannelDefinition();
            s.ContentType = TestContext.CurrentContext.Random.GetString();
            s.DisplayName = TestContext.CurrentContext.Random.GetString();
            s.Enabled = true;
            s.HttpMethod = TestContext.CurrentContext.Random.GetString();
            s.Id = TestContext.CurrentContext.Random.GetString();
            s.Mandatory = true;
            s.TemplateFailure = TestContext.CurrentContext.Random.GetString();
            s.TemplateSuccess = TestContext.CurrentContext.Random.GetString();
            s.Url = TestContext.CurrentContext.Random.GetString();

            WebhookNotificationChannelDefinition n = JsonConvert.DeserializeObject<WebhookNotificationChannelDefinition>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.ContentType, n.ContentType);
            Assert.AreEqual(s.DisplayName, n.DisplayName);
            Assert.AreEqual(s.Enabled, n.Enabled);
            Assert.AreEqual(s.HttpMethod, n.HttpMethod);
            Assert.AreEqual(s.Id, n.Id);
            Assert.AreEqual(s.Mandatory, n.Mandatory);
            Assert.AreEqual(s.TemplateFailure, n.TemplateFailure);
            Assert.AreEqual(s.TemplateSuccess, n.TemplateSuccess);
            Assert.AreEqual(s.Url, n.Url);
        }

        [Test]
        public void SerializeUserInterfaceOptions()
        {
            UserInterfaceOptions s = new UserInterfaceOptions();
            s.Title = TestContext.CurrentContext.Random.GetString();
            s.UserSuppliedReason = AuditReasonFieldState.Required;
            s.AllowJit = true;
            s.AllowLaps = true;
            s.AllowLapsHistory = true;
            s.PhoneticSettings.GroupSize = 5;
            s.PhoneticSettings.LowerPrefix = TestContext.CurrentContext.Random.GetString();
            s.PhoneticSettings.UpperPrefix = TestContext.CurrentContext.Random.GetString();
            s.PhoneticSettings.PhoneticNameColon = TestContext.CurrentContext.Random.GetString();
            s.PhoneticSettings.CharacterMappings = new Dictionary<string, string>();
            s.PhoneticSettings.CharacterMappings.Add(TestContext.CurrentContext.Random.GetString(),
                TestContext.CurrentContext.Random.GetString());

            UserInterfaceOptions n = JsonConvert.DeserializeObject<UserInterfaceOptions>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.Title, n.Title);
            Assert.AreEqual(s.UserSuppliedReason, n.UserSuppliedReason);
            Assert.AreEqual(s.AllowJit, n.AllowJit);
            Assert.AreEqual(s.AllowLaps, n.AllowLaps);
            Assert.AreEqual(s.AllowLapsHistory, n.AllowLapsHistory);
            Assert.AreEqual(s.PhoneticSettings.GroupSize, n.PhoneticSettings.GroupSize);
            Assert.AreEqual(s.PhoneticSettings.LowerPrefix, n.PhoneticSettings.LowerPrefix);
            Assert.AreEqual(s.PhoneticSettings.UpperPrefix, n.PhoneticSettings.UpperPrefix);
            Assert.AreEqual(s.PhoneticSettings.PhoneticNameColon, n.PhoneticSettings.PhoneticNameColon);
            Assert.AreEqual(s.PhoneticSettings.CharacterMappings.First().Key,
                n.PhoneticSettings.CharacterMappings.First().Key);
            Assert.AreEqual(s.PhoneticSettings.CharacterMappings.First().Value,
                n.PhoneticSettings.CharacterMappings.First().Value);
        }

        [Test]
        public void SerializeSmtpNotificationChannelDefinition()
        {
            SmtpNotificationChannelDefinition s = new SmtpNotificationChannelDefinition();
            s.DisplayName = TestContext.CurrentContext.Random.GetString();
            s.Enabled = true;
            s.Mandatory = true;
            s.Id = TestContext.CurrentContext.Random.GetString();
            s.TemplateFailure = TestContext.CurrentContext.Random.GetString();
            s.TemplateSuccess = TestContext.CurrentContext.Random.GetString();
            s.EmailAddresses.Add(TestContext.CurrentContext.Random.GetString());

            SmtpNotificationChannelDefinition n = JsonConvert.DeserializeObject<SmtpNotificationChannelDefinition>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.DisplayName, n.DisplayName);
            Assert.AreEqual(s.Enabled, n.Enabled);
            Assert.AreEqual(s.Mandatory, n.Mandatory);
            Assert.AreEqual(s.Id, n.Id);
            Assert.AreEqual(s.TemplateFailure, n.TemplateFailure);
            Assert.AreEqual(s.TemplateSuccess, n.TemplateSuccess);
            CollectionAssert.AreEqual(s.EmailAddresses, n.EmailAddresses);
        }

        [Test]
        public void SerializePowershellNotificationChannelDefinition()
        {
            PowershellNotificationChannelDefinition s = new PowershellNotificationChannelDefinition();
            s.DisplayName = TestContext.CurrentContext.Random.GetString();
            s.Enabled = true;
            s.Mandatory = true;
            s.Id = TestContext.CurrentContext.Random.GetString();
            s.Script = TestContext.CurrentContext.Random.GetString();
            s.TimeOut = 44;

            PowershellNotificationChannelDefinition n = JsonConvert.DeserializeObject<PowershellNotificationChannelDefinition>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.DisplayName, n.DisplayName);
            Assert.AreEqual(s.Enabled, n.Enabled);
            Assert.AreEqual(s.Mandatory, n.Mandatory);
            Assert.AreEqual(s.Id, n.Id);
            Assert.AreEqual(s.Script, n.Script);
            Assert.AreEqual(s.TimeOut, n.TimeOut);
        }

        [Test]
        public void SerializeRateLimitThresholds()
        {
            RateLimitThresholds s = new RateLimitThresholds();
            s.Enabled = true;
            s.RequestsPerDay = TestContext.CurrentContext.Random.Next();
            s.RequestsPerHour = TestContext.CurrentContext.Random.Next();
            s.RequestsPerMinute = TestContext.CurrentContext.Random.Next();

            RateLimitThresholds n = JsonConvert.DeserializeObject<RateLimitThresholds>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.Enabled, n.Enabled);
            Assert.AreEqual(s.RequestsPerDay, n.RequestsPerDay);
            Assert.AreEqual(s.RequestsPerHour, n.RequestsPerHour);
            Assert.AreEqual(s.RequestsPerMinute, n.RequestsPerMinute);
        }

        [Test]
        public void SerializeRateLimitOptions()
        {
            RateLimitOptions s = new RateLimitOptions();
            s.PerIP.Enabled = true;
            s.PerIP.RequestsPerDay = TestContext.CurrentContext.Random.Next();
            s.PerIP.RequestsPerHour = TestContext.CurrentContext.Random.Next();
            s.PerIP.RequestsPerMinute = TestContext.CurrentContext.Random.Next();

            s.PerUser.Enabled = true;
            s.PerUser.RequestsPerDay = TestContext.CurrentContext.Random.Next();
            s.PerUser.RequestsPerHour = TestContext.CurrentContext.Random.Next();
            s.PerUser.RequestsPerMinute = TestContext.CurrentContext.Random.Next();

            RateLimitOptions n = JsonConvert.DeserializeObject<RateLimitOptions>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.PerIP.Enabled, n.PerIP.Enabled);
            Assert.AreEqual(s.PerIP.RequestsPerDay, n.PerIP.RequestsPerDay);
            Assert.AreEqual(s.PerIP.RequestsPerHour, n.PerIP.RequestsPerHour);
            Assert.AreEqual(s.PerIP.RequestsPerMinute, n.PerIP.RequestsPerMinute);
            Assert.AreEqual(s.PerUser.Enabled, n.PerUser.Enabled);
            Assert.AreEqual(s.PerUser.RequestsPerDay, n.PerUser.RequestsPerDay);
            Assert.AreEqual(s.PerUser.RequestsPerHour, n.PerUser.RequestsPerHour);
            Assert.AreEqual(s.PerUser.RequestsPerMinute, n.PerUser.RequestsPerMinute);
        }

        [Test]
        public void SerializeOidcAuthenticationProviderOptions()
        {
            OidcAuthenticationProviderOptions s = new OidcAuthenticationProviderOptions();
            s.ClaimName = TestContext.CurrentContext.Random.GetString();
            s.IdpLogout = true;
            s.Authority = TestContext.CurrentContext.Random.GetString();
            s.ClientID = TestContext.CurrentContext.Random.GetString();
            s.ResponseType = TestContext.CurrentContext.Random.GetString();
            s.Secret = TestContext.CurrentContext.Random.GetString();
            s.Scopes = new List<string>
            {
                TestContext.CurrentContext.Random.GetString()
            };

            OidcAuthenticationProviderOptions n = JsonConvert.DeserializeObject<OidcAuthenticationProviderOptions>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.ClaimName, n.ClaimName);
            Assert.AreEqual(s.IdpLogout, n.IdpLogout);
            Assert.AreEqual(s.Authority, n.Authority);
            Assert.AreEqual(s.ClientID, n.ClientID);
            Assert.AreEqual(s.ResponseType, n.ResponseType);
            Assert.AreEqual(s.Secret, n.Secret);
            CollectionAssert.AreEqual(s.Scopes, n.Scopes);
        }

        [Test]
        public void SerializeJitGroupMapping()
        {
            JitGroupMapping s = new JitGroupMapping();
            s.ComputerOU = TestContext.CurrentContext.Random.GetString();
            s.GroupOU = TestContext.CurrentContext.Random.GetString();
            s.GroupNameTemplate = TestContext.CurrentContext.Random.GetString();
            s.GroupType = GroupType.Global;

            JitGroupMapping n = JsonConvert.DeserializeObject<JitGroupMapping>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.ComputerOU, n.ComputerOU);
            Assert.AreEqual(s.GroupOU, n.GroupOU);
            Assert.AreEqual(s.GroupNameTemplate, n.GroupNameTemplate);
            Assert.AreEqual(s.GroupType, n.GroupType);
        }

        [Test]
        public void SerializeJitConfigurationOptions()
        {

            JitConfigurationOptions s = new JitConfigurationOptions();
            s.EnableJitGroupCreation = true;
            s.DeltaSyncInterval = TestContext.CurrentContext.Random.Next();
            s.FullSyncInterval = TestContext.CurrentContext.Random.Next();

            s.JitGroupMappings = new List<JitGroupMapping>
            {
                new JitGroupMapping
                {
                    ComputerOU = TestContext.CurrentContext.Random.GetString(),
                    GroupOU = TestContext.CurrentContext.Random.GetString(),
                    GroupNameTemplate = TestContext.CurrentContext.Random.GetString(),
                    GroupType = GroupType.Universal,
                    EnableJitGroupDeletion = false,
                    GroupDescription =  TestContext.CurrentContext.Random.GetString(),
                    PreferredDC =  TestContext.CurrentContext.Random.GetString(),
                    Subtree = true
                }
            };

            JitConfigurationOptions n = JsonConvert.DeserializeObject<JitConfigurationOptions>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.EnableJitGroupCreation, n.EnableJitGroupCreation);
            Assert.AreEqual(s.DeltaSyncInterval, n.DeltaSyncInterval);
            Assert.AreEqual(s.FullSyncInterval, n.FullSyncInterval);
            Assert.AreEqual(s.JitGroupMappings[0].ComputerOU, n.JitGroupMappings[0].ComputerOU);
            Assert.AreEqual(s.JitGroupMappings[0].GroupOU, n.JitGroupMappings[0].GroupOU);
            Assert.AreEqual(s.JitGroupMappings[0].GroupNameTemplate, n.JitGroupMappings[0].GroupNameTemplate);
            Assert.AreEqual(s.JitGroupMappings[0].GroupType, n.JitGroupMappings[0].GroupType);
            Assert.AreEqual(s.JitGroupMappings[0].EnableJitGroupDeletion, n.JitGroupMappings[0].EnableJitGroupDeletion);
            Assert.AreEqual(s.JitGroupMappings[0].GroupDescription, n.JitGroupMappings[0].GroupDescription);
            Assert.AreEqual(s.JitGroupMappings[0].PreferredDC, n.JitGroupMappings[0].PreferredDC);
            Assert.AreEqual(s.JitGroupMappings[0].Subtree, n.JitGroupMappings[0].Subtree);
        }

        [Test]
        public void SerializeIwaAuthenticationProviderOptions()
        {
            IwaAuthenticationProviderOptions s = new IwaAuthenticationProviderOptions();
            s.AuthenticationSchemes = AuthenticationSchemes.NTLM;

            IwaAuthenticationProviderOptions n = JsonConvert.DeserializeObject<IwaAuthenticationProviderOptions>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.AuthenticationSchemes, n.AuthenticationSchemes);
        }

        [Test]
        public void SerializeHttpSysHostingOptions()
        {
            HttpSysHostingOptions s = new HttpSysHostingOptions();
            s.AllowSynchronousIO = true;
            s.EnableResponseCaching = true;
            s.Hostname = TestContext.CurrentContext.Random.GetString();
            s.Http503Verbosity = Http503VerbosityLevel.Full;
            s.HttpPort = TestContext.CurrentContext.Random.Next();
            s.HttpsPort = TestContext.CurrentContext.Random.Next();
            s.MaxAccepts = TestContext.CurrentContext.Random.Next();
            s.MaxConnections = TestContext.CurrentContext.Random.Next();
            s.MaxRequestBodySize = TestContext.CurrentContext.Random.Next();
            s.Path = TestContext.CurrentContext.Random.GetString();
            s.RequestQueueLimit = TestContext.CurrentContext.Random.Next();
            s.ThrowWriteExceptions = false;

            HttpSysHostingOptions n = JsonConvert.DeserializeObject<HttpSysHostingOptions>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.AllowSynchronousIO, n.AllowSynchronousIO);
            Assert.AreEqual(s.EnableResponseCaching, n.EnableResponseCaching);
            Assert.AreEqual(s.Hostname, n.Hostname);
            Assert.AreEqual(s.Http503Verbosity, n.Http503Verbosity);
            Assert.AreEqual(s.HttpPort, n.HttpPort);
            Assert.AreEqual(s.HttpsPort, n.HttpsPort);
            Assert.AreEqual(s.MaxAccepts, n.MaxAccepts);
            Assert.AreEqual(s.MaxConnections, n.MaxConnections);
            Assert.AreEqual(s.MaxRequestBodySize, n.MaxRequestBodySize);
            Assert.AreEqual(s.Path, n.Path);
            Assert.AreEqual(s.RequestQueueLimit, n.RequestQueueLimit);
            Assert.AreEqual(s.ThrowWriteExceptions, n.ThrowWriteExceptions);
        }

        [Test]
        public void SerializeHostingOptions()
        {
            HostingOptions s = new HostingOptions();
            s.Environment = HostingEnvironment.HttpSys;

            HostingOptions n = JsonConvert.DeserializeObject<HostingOptions>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.Environment, n.Environment);
        }

        [Test]
        public void SerializeForwardedHeadersAppOptions()
        {
            ForwardedHeadersAppOptions s = new ForwardedHeadersAppOptions();
            s.AllowedHosts = new List<string>()
            {
                TestContext.CurrentContext.Random.GetString(), TestContext.CurrentContext.Random.GetString()
            };

            s.ForwardedForHeaderName = TestContext.CurrentContext.Random.GetString();
            s.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor;
            s.ForwardedHostHeaderName = TestContext.CurrentContext.Random.GetString();
            s.ForwardedProtoHeaderName = TestContext.CurrentContext.Random.GetString();
            s.ForwardLimit = TestContext.CurrentContext.Random.Next();
            s.KnownNetworks = new List<string>()
            {
                TestContext.CurrentContext.Random.GetString(),TestContext.CurrentContext.Random.GetString()
            };
            s.KnownProxies = new List<string>()
            {
                TestContext.CurrentContext.Random.GetString(),TestContext.CurrentContext.Random.GetString()
            };
            s.OriginalForHeaderName = TestContext.CurrentContext.Random.GetString();
            s.OriginalHostHeaderName = TestContext.CurrentContext.Random.GetString();
            s.OriginalProtoHeaderName = TestContext.CurrentContext.Random.GetString();
            s.RequireHeaderSymmetry = true;

            ForwardedHeadersAppOptions n = JsonConvert.DeserializeObject<ForwardedHeadersAppOptions>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.ForwardedForHeaderName, n.ForwardedForHeaderName);
            Assert.AreEqual(s.ForwardedHeaders, n.ForwardedHeaders);
            Assert.AreEqual(s.ForwardedHostHeaderName, n.ForwardedHostHeaderName);
            Assert.AreEqual(s.ForwardedProtoHeaderName, n.ForwardedProtoHeaderName);
            Assert.AreEqual(s.ForwardLimit, n.ForwardLimit);
            Assert.AreEqual(s.OriginalForHeaderName, n.OriginalForHeaderName);
            Assert.AreEqual(s.OriginalHostHeaderName, n.OriginalHostHeaderName);
            Assert.AreEqual(s.OriginalProtoHeaderName, n.OriginalProtoHeaderName);
            Assert.AreEqual(s.RequireHeaderSymmetry, n.RequireHeaderSymmetry);

            CollectionAssert.AreEqual(s.AllowedHosts, n.AllowedHosts);
            CollectionAssert.AreEqual(s.KnownNetworks, n.KnownNetworks);
            CollectionAssert.AreEqual(s.KnownProxies, n.KnownProxies);
        }

        [Test]
        public void ForwardedHeadersAppOptionsToNativeOptions()
        {
            ForwardedHeadersAppOptions s = new ForwardedHeadersAppOptions();
            s.AllowedHosts = new List<string>()
            {
                "2.2.2.2"
            };

            s.ForwardedForHeaderName = TestContext.CurrentContext.Random.GetString();
            s.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor;
            s.ForwardedHostHeaderName = TestContext.CurrentContext.Random.GetString();
            s.ForwardedProtoHeaderName = TestContext.CurrentContext.Random.GetString();
            s.ForwardLimit = TestContext.CurrentContext.Random.Next();
            s.KnownNetworks = new List<string>()
            {
               "1.1.1.1/5"
            };
            s.KnownProxies = new List<string>()
            {
                "1.2.3.4"
            };
            s.OriginalForHeaderName = TestContext.CurrentContext.Random.GetString();
            s.OriginalHostHeaderName = TestContext.CurrentContext.Random.GetString();
            s.OriginalProtoHeaderName = TestContext.CurrentContext.Random.GetString();
            s.RequireHeaderSymmetry = true;

            var n = s.ToNativeOptions();

            Assert.AreEqual(s.ForwardedForHeaderName, n.ForwardedForHeaderName);
            Assert.AreEqual(s.ForwardedHeaders, n.ForwardedHeaders);
            Assert.AreEqual(s.ForwardedHostHeaderName, n.ForwardedHostHeaderName);
            Assert.AreEqual(s.ForwardedProtoHeaderName, n.ForwardedProtoHeaderName);
            Assert.AreEqual(s.ForwardLimit, n.ForwardLimit);
            Assert.AreEqual(s.OriginalForHeaderName, n.OriginalForHeaderName);
            Assert.AreEqual(s.OriginalHostHeaderName, n.OriginalHostHeaderName);
            Assert.AreEqual(s.OriginalProtoHeaderName, n.OriginalProtoHeaderName);
            Assert.AreEqual(s.RequireHeaderSymmetry, n.RequireHeaderSymmetry);

            CollectionAssert.AreEqual(s.AllowedHosts, n.AllowedHosts);
            CollectionAssert.AreEqual(s.KnownNetworks, n.KnownNetworks.Select(t => $"{t.Prefix}/{t.PrefixLength}"));
            CollectionAssert.AreEqual(s.KnownProxies, n.KnownProxies.Select(t => t.ToString()));
        }

        [Test]
        public void SerializeEmailOptions()
        {
            EmailOptions s = new EmailOptions();
            s.FromAddress = TestContext.CurrentContext.Random.GetString();
            s.Host = TestContext.CurrentContext.Random.GetString();
            s.Password = TestContext.CurrentContext.Random.GetString();
            s.Port = TestContext.CurrentContext.Random.Next();
            s.UseDefaultCredentials = true;
            s.Username = TestContext.CurrentContext.Random.GetString();
            s.UseSsl = true;

            EmailOptions n = JsonConvert.DeserializeObject<EmailOptions>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.FromAddress, n.FromAddress);
            Assert.AreEqual(s.Host, n.Host);
            Assert.AreEqual(s.Password, n.Password);
            Assert.AreEqual(s.Port, n.Port);
            Assert.AreEqual(s.UseDefaultCredentials, n.UseDefaultCredentials);
            Assert.AreEqual(s.Username, n.Username);
            Assert.AreEqual(s.UseSsl, n.UseSsl);
        }

        [Test]
        public void SerializeSecurityDescriptorTarget()
        {
            SecurityDescriptorTarget s = new SecurityDescriptorTarget();
            s.AuthorizationMode = AuthorizationMode.PowershellScript;
            s.Description = TestContext.CurrentContext.Random.GetString();
            s.Id = TestContext.CurrentContext.Random.GetString();
            s.Script = TestContext.CurrentContext.Random.GetString();
            s.SecurityDescriptor = TestContext.CurrentContext.Random.GetString();
            s.Target = TestContext.CurrentContext.Random.GetString();
            s.Type = TargetType.Container;
            s.Laps.ExpireAfter = TimeSpan.FromSeconds(TestContext.CurrentContext.Random.Next());
            s.Laps.RetrievalLocation = PasswordStorageLocation.LithnetAttribute;
            s.Jit.AuthorizingGroup = TestContext.CurrentContext.Random.GetString();
            s.Jit.ExpireAfter = TimeSpan.FromSeconds(TestContext.CurrentContext.Random.Next());
            s.Notifications.OnFailure.Add(TestContext.CurrentContext.Random.GetString());
            s.Notifications.OnSuccess.Add(TestContext.CurrentContext.Random.GetString());

            SecurityDescriptorTarget n = JsonConvert.DeserializeObject<SecurityDescriptorTarget>(JsonConvert.SerializeObject(s));

            Assert.AreEqual(s.AuthorizationMode, n.AuthorizationMode);
            Assert.AreEqual(s.Description, n.Description);
            Assert.AreEqual(s.Id, n.Id);
            Assert.AreEqual(s.Script, n.Script);
            Assert.AreEqual(s.SecurityDescriptor, n.SecurityDescriptor);
            Assert.AreEqual(s.Target, n.Target);
            Assert.AreEqual(s.Type, n.Type);
            Assert.AreEqual(s.Laps.ExpireAfter, n.Laps.ExpireAfter);
            Assert.AreEqual(s.Laps.RetrievalLocation, n.Laps.RetrievalLocation);
            Assert.AreEqual(s.Jit.AuthorizingGroup, n.Jit.AuthorizingGroup);
            Assert.AreEqual(s.Jit.ExpireAfter, n.Jit.ExpireAfter);
            CollectionAssert.AreEqual(s.Notifications.OnFailure, n.Notifications.OnFailure);
            CollectionAssert.AreEqual(s.Notifications.OnSuccess, n.Notifications.OnSuccess);
        }
    }
}
