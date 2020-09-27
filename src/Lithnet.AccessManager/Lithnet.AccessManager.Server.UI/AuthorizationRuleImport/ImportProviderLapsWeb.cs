using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Xml;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class ImportProviderLapsWeb : IImportProvider
    {
        private readonly ILogger logger;
        private readonly IDirectory directory;
        private readonly ImportSettingsLapsWeb settings;

        public event EventHandler<ImportProcessingEventArgs> OnItemProcessStart;

        public event EventHandler<ImportProcessingEventArgs> OnItemProcessFinish;

        public ImportProviderLapsWeb(ImportSettingsLapsWeb settings, ILogger<ImportProviderLapsWeb> logger, IDirectory directory)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
        }

        public int GetEstimatedItemCount()
        {
            return 0;
        }

        public ImportResults Import()
        {
            ImportResults results = new ImportResults();
            SmtpNotificationChannelDefinition globalChannel = null;
            bool onSuccessGlobal = false;
            bool onFailureGlobal = false;

            string xml = File.ReadAllText(settings.ImportFile);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode appRootNode = doc.SelectSingleNode("/configuration/lithnet-laps");

            if (appRootNode == null)
            {
                throw new ImportException("The specified file did not appear to be a Lithnet LAPS Web App config file");
            }

            if (settings.ImportNotifications)
            {
                globalChannel = this.GetNotificationChannelDefinition(appRootNode, out onSuccessGlobal, out onFailureGlobal);

                if (globalChannel != null)
                {
                    results.NotificationChannels.Smtp.Add(globalChannel);
                }
            }

            XmlNodeList targetNodes = doc.SelectNodes("/configuration/lithnet-laps/targets/target");

            if (targetNodes == null || targetNodes.Count == 0)
            {
                return results;
            }

            foreach (XmlElement targetNode in targetNodes.OfType<XmlElement>())
            {
                this.OnItemProcessStart?.Invoke(this, new ImportProcessingEventArgs($"Processing {targetNode.SelectSingleNode("@name")?.Value}"));

                SecurityDescriptorTarget target = this.ConvertToSecurityDescriptorTarget(targetNode, out List<DiscoveryError> discoveryErrors);

                if (discoveryErrors.Count > 0)
                {
                    results.DiscoveryErrors.AddRange(discoveryErrors);
                }

                if (target == null)
                {
                    continue;
                }

                if (settings.ImportNotifications)
                {
                    SmtpNotificationChannelDefinition channel = this.GetNotificationChannelDefinition(targetNode, out bool onSuccess, out bool onFailure);

                    if (channel != null)
                    {
                        if (onSuccess)
                        {
                            target.Notifications.OnSuccess.Add(channel.Id);
                        }

                        if (onFailure)
                        {
                            target.Notifications.OnFailure.Add(channel.Id);
                        }

                        results.NotificationChannels.Smtp.Add(channel);
                    }

                    if (onSuccessGlobal && globalChannel != null)
                    {
                        target.Notifications.OnSuccess.Add(globalChannel.Id);
                    }

                    if (onFailureGlobal && globalChannel != null)
                    {
                        target.Notifications.OnFailure.Add(globalChannel.Id);
                    }
                }

                results.Targets.Add(target);

                this.OnItemProcessFinish?.Invoke(this, new ImportProcessingEventArgs($"Processing {targetNode.SelectSingleNode("@name")?.Value}"));
            }

            return results;
        }

        private SecurityDescriptorTarget ConvertToSecurityDescriptorTarget(XmlElement node, out List<DiscoveryError> discoveryErrors)
        {
            discoveryErrors = new List<DiscoveryError>();
            SecurityDescriptorTarget target = new SecurityDescriptorTarget();

            string name = node.SelectSingleNode("@name")?.Value;
            string type = node.SelectSingleNode("@type")?.Value;
            string expireAfter = node.SelectSingleNode("@expire-after")?.Value;
            List<string> readers = node.SelectNodes("readers/reader/@principal")?.OfType<XmlAttribute>().Select(t => t.Value).ToList();

            if (string.IsNullOrWhiteSpace(name))
            {
                this.logger.LogWarning("XmlElement had a null name");
                return null;
            }

            if (string.Equals(type, "container", StringComparison.OrdinalIgnoreCase))
            {
                target.Type = TargetType.Container;
                target.Target = name;
            }
            else if (string.Equals(type, "computer", StringComparison.OrdinalIgnoreCase))
            {
                target.Type = TargetType.Computer;

                if (this.directory.TryGetComputer(name, out IComputer computer))
                {
                    target.Target = computer.Sid.ToString();
                }
                else
                {
                    discoveryErrors.Add(new DiscoveryError { Target = name, Type = DiscoveryErrorType.Error, Message = $"The computer was not found in the directory" });
                    return null;
                }
            }
            else if (string.Equals(type, "group", StringComparison.OrdinalIgnoreCase))
            {
                target.Type = TargetType.Group;

                if (this.directory.TryGetGroup(name, out IGroup group))
                {
                    target.Target = group.Sid.ToString();
                }
                else
                {
                    discoveryErrors.Add(new DiscoveryError { Target = name, Type = DiscoveryErrorType.Error, Message = $"The group was not found in the directory" });
                    return null;
                }
            }
            else
            {
                discoveryErrors.Add(new DiscoveryError { Target = name, Type = DiscoveryErrorType.Error, Message = $"Target was of an unknown type: {type}" });
                return null;
            }

            if (readers == null || readers.Count == 0)
            {
                discoveryErrors.Add(new DiscoveryError { Target = name, Type = DiscoveryErrorType.Warning, Message = $"Target had not authorized readers" });
                return null;
            }

            if (!string.IsNullOrWhiteSpace(expireAfter))
            {
                if (TimeSpan.TryParse(expireAfter, out TimeSpan timespan))
                {
                    if (timespan.TotalMinutes > 0)
                    {
                        target.Laps.ExpireAfter = timespan;
                    }
                }
            }

            target.AuthorizationMode = AuthorizationMode.SecurityDescriptor;
            target.Description = settings.RuleDescription;

            foreach (var onSuccess in settings.Notifications.OnSuccess)
            {
                target.Notifications.OnSuccess.Add(onSuccess);
            }

            foreach (var onFailure in settings.Notifications.OnFailure)
            {
                target.Notifications.OnFailure.Add(onFailure);
            }

            AccessMask mask = AccessMask.LocalAdminPassword;
            mask |= settings.AllowLapsHistory ? AccessMask.LocalAdminPasswordHistory : 0;
            mask |= settings.AllowJit ? AccessMask.Jit : 0;
            mask |= settings.AllowBitLocker ? AccessMask.BitLocker : 0;

            DiscretionaryAcl acl = new DiscretionaryAcl(false, false, readers.Count);

            foreach (string reader in readers)
            {
                if (directory.TryGetPrincipal(reader, out ISecurityPrincipal principal))
                {
                    acl.AddAccess(AccessControlType.Allow, principal.Sid, (int)mask, InheritanceFlags.None, PropagationFlags.None);
                }
                else
                {
                    discoveryErrors.Add(new DiscoveryError { Target = name, Type = DiscoveryErrorType.Warning, Principal = reader, Message = $"The principal could not be found in the directory" });
                }
            }

            if (acl.Count == 0)
            {
                discoveryErrors.Add(new DiscoveryError { Target = name, Type = DiscoveryErrorType.Warning, Message = $"Target had no authorized readers" });
                return null;
            }

            CommonSecurityDescriptor sd = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), null, null, acl);

            target.SecurityDescriptor = sd.GetSddlForm(AccessControlSections.All);

            return target;
        }

        private SmtpNotificationChannelDefinition GetNotificationChannelDefinition(XmlNode node, out bool onSuccess, out bool onFailure)
        {
            onSuccess = false;
            onFailure = false;

            XmlNode notificationNode = node.SelectSingleNode("audit");

            if (notificationNode != null)
            {
                string addresses = notificationNode.SelectSingleNode("@emailAddresses")?.Value;
                string displayName = $"Send email to {addresses}";

                bool.TryParse(notificationNode.SelectSingleNode("@emailOnSuccess")?.Value, out onSuccess);
                bool.TryParse(notificationNode.SelectSingleNode("@emailOnFailure")?.Value, out onFailure);

                if (onSuccess || onFailure)
                {
                    SmtpNotificationChannelDefinition channel = new SmtpNotificationChannelDefinition
                    {
                        DisplayName = displayName,
                        Enabled = true,
                        Mandatory = false,
                        EmailAddresses = addresses?.Split(',', ';', StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? new List<string>(),
                        TemplateSuccess = settings.SuccessTemplate,
                        TemplateFailure = settings.FailureTemplate
                    };

                    if (channel.EmailAddresses.Count > 0)
                    {
                        return channel;
                    }
                }
            }

            return null;
        }
    }
}
