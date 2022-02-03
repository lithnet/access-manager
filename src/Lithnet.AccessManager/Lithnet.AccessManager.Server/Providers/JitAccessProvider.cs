using System;
using System.Security.Principal;
using Lithnet.AccessManager.Interop;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server
{
    public class JitAccessProvider : IJitAccessProvider
    {
        private readonly IDirectory directory;
        private readonly ILogger<JitAccessProvider> logger;
        private readonly JitConfigurationOptions options;
        private readonly IDiscoveryServices discoveryServices;

        public JitAccessProvider(IDirectory directory, ILogger<JitAccessProvider> logger, IOptionsSnapshot<JitConfigurationOptions> options, IDiscoveryServices discoveryServices)
        {
            this.directory = directory;
            this.logger = logger;
            this.options = options.Value;
            this.discoveryServices = discoveryServices;
        }

        public TimeSpan GrantJitAccess(IGroup group, IUser user, IComputer computer, bool canExtend, TimeSpan requestedExpiry, out Action undo)
        {
            requestedExpiry = requestedExpiry.Ticks <= 0 ? TimeSpan.FromMinutes(5) : requestedExpiry;

            this.logger.LogTrace("Adding user {user} to JIT group {group}", user.MsDsPrincipalName, group.MsDsPrincipalName);

            if (this.directory.IsPamFeatureEnabled(group.Sid, false))
            {
                return this.GrantJitAccessPam(group, user, this.GetDcLocatorTarget(computer), canExtend, requestedExpiry, out undo);
            }
            else
            {
                return this.GrantJitAccessDynamicGroup(group, user, this.GetDcLocatorTarget(computer), canExtend, requestedExpiry, out undo);
            }
        }

        public TimeSpan GrantJitAccess(IGroup group, IUser user, bool canExtend, TimeSpan requestedExpiry, out Action undo)
        {
            requestedExpiry = requestedExpiry.Ticks <= 0 ? TimeSpan.FromMinutes(5) : requestedExpiry;

            this.logger.LogTrace("Adding user {user} to JIT group {group}", user.MsDsPrincipalName, group.MsDsPrincipalName);

            if (this.directory.IsPamFeatureEnabled(group.Sid, false))
            {
                return this.GrantJitAccessPam(group, user, null, canExtend, requestedExpiry, out undo);
            }
            else
            {
                return this.GrantJitAccessDynamicGroup(group, user, null, canExtend, requestedExpiry, out undo);
            }
        }

        public TimeSpan GrantJitAccessDynamicGroup(IGroup group, IUser user, string dcLocatorTarget, bool canExtend, TimeSpan requestedExpiry, out Action undo)
        {
            JitDynamicGroupMapping mapping = this.FindDomainMapping(group);
            string groupName = this.BuildGroupSamAccountName(mapping, user, group);
            string description = this.BuildGroupDescription(mapping);
            string fqGroupName = $"{this.BuildGroupDomain(group)}\\{groupName}";

            TimeSpan grantedExpiry = requestedExpiry;

            this.logger.LogTrace("Processing request to have {user} added to the JIT group {group} via dynamicObject {dynamicGroup}", user.MsDsPrincipalName, group.Path, fqGroupName);

            IGroup dynamicGroup = null;

            this.discoveryServices.FindDcAndExecuteWithRetry(dcLocatorTarget, this.discoveryServices.GetDomainNameDns(mapping.GroupOU), DsGetDcNameFlags.DS_DIRECTORY_SERVICE_REQUIRED | DsGetDcNameFlags.DS_WRITABLE_REQUIRED, this.GetDcLocatorMode(), dc =>
            {
                this.logger.LogTrace("Attempting to perform dynamic group operation against DC {dc}", dc);

                group.RetargetToDc(dc);

                if (directory.TryGetGroup(fqGroupName, out dynamicGroup))
                {
                    dynamicGroup.RetargetToDc(dc);

                    this.logger.LogTrace("Dynamic group {dynamicGroup} already exists in the directory with a remaining TTL of {ttl}", dynamicGroup.Path, dynamicGroup.EntryTtl);

                    if (!canExtend)
                    {
                        this.logger.LogTrace("User {user} is not permitted to extend the access, so the TTL will remain unchanged", user.MsDsPrincipalName);
                        grantedExpiry = dynamicGroup.EntryTtl ?? new TimeSpan();
                    }
                    else
                    {
                        dynamicGroup.ExtendTtl(requestedExpiry);
                        this.logger.LogTrace("User {user} is permitted to extend the access, so the TTL will was updated to {ttl}", user.MsDsPrincipalName, requestedExpiry);
                    }
                }
                else
                {
                    this.logger.LogTrace("Creating a new dynamic group {groupName} in {ou} with TTL of {ttl}", groupName, mapping.GroupOU, grantedExpiry);
                    dynamicGroup = this.directory.CreateTtlGroup(groupName, groupName, description, mapping.GroupOU, dc, grantedExpiry, mapping.GroupType, true);
                    this.logger.LogInformation(EventIDs.JitDynamicGroupCreated, "Created a new dynamic group {group} on domain controller {dc}", dynamicGroup.Path, grantedExpiry, dc);
                }

                this.logger.LogTrace("Adding user {user} to dynamic group {dynamicGroup} on domain controller {dc}", user.MsDsPrincipalName, dynamicGroup.Path, dc);
                dynamicGroup.AddMember(user);

                this.logger.LogTrace("Adding dynamic group {dynamicGroup} to the JIT group {jitGroup} on domain controller {dc}", dynamicGroup.Path, group.Path, dc);
                group.AddMember(dynamicGroup);

                return true;
            });

            undo = () =>
            {
                if (dynamicGroup != null)
                {
                    this.logger.LogTrace("Rolling back JIT access by deleting dynamic group {dynamicGroup} created for {user} to become a member of {group}", dynamicGroup?.MsDsPrincipalName, user.MsDsPrincipalName, group.MsDsPrincipalName);
                    this.directory.DeleteGroup(fqGroupName);
                    this.logger.LogInformation(EventIDs.JitDynamicGroupDeleted, "Rolled back JIT access by deleting dynamic group {dynamicGroup} created for {user} to become a member of {group}", dynamicGroup?.MsDsPrincipalName, user.MsDsPrincipalName, group.MsDsPrincipalName);
                }
            };

            return grantedExpiry;
        }

        public TimeSpan GrantJitAccessPam(IGroup group, IUser user, string dcLocatorTarget, bool canExtend, TimeSpan requestedExpiry, out Action undo)
        {
            TimeSpan? existingTtl = group.GetMemberTtl(user);

            if (existingTtl != null)
            {
                this.logger.LogTrace("User {user} is already a member of {group} with {ttl} left remaining on their membership", user.MsDsPrincipalName, group.MsDsPrincipalName, existingTtl.Value);
            }

            if (existingTtl != null && !canExtend)
            {
                this.logger.LogTrace("User {user} is not allowed to extend their access window in group {group}", user.MsDsPrincipalName, group.MsDsPrincipalName);
                undo = () => { };
                return existingTtl.Value;
            }

            this.discoveryServices.FindDcAndExecuteWithRetry(dcLocatorTarget, this.discoveryServices.GetDomainNameDns(group.Sid), DsGetDcNameFlags.DS_DIRECTORY_SERVICE_REQUIRED | DsGetDcNameFlags.DS_WRITABLE_REQUIRED, this.GetDcLocatorMode(), dc =>
            {
                this.logger.LogTrace("Attempting to perform pam group operation against DC {dc}", dc);
                group.RetargetToDc(dc);

                this.logger.LogTrace("Adding user {user} to group {group}", user.MsDsPrincipalName, group.Path);

                group.AddMember(user, requestedExpiry);
                this.logger.LogInformation(EventIDs.JitPamAccessGranted, "User {user} was added to group {group} with a membership expiry of {ttl} on domain controller {dc}", user.MsDsPrincipalName, group.MsDsPrincipalName, requestedExpiry, dc);

                return true;
            });

            undo = () =>
            {
                this.logger.LogTrace("Rolling back JIT access by removing {user} from {group}", user.MsDsPrincipalName, group.MsDsPrincipalName);
                group.RemoveMember(user);
                this.logger.LogInformation(EventIDs.JitPamAccessRevoked, "Rolled back JIT access by removing {user} from {group}", user.MsDsPrincipalName, group.MsDsPrincipalName);

            };

            return requestedExpiry;
        }

        private string BuildGroupDescription(JitDynamicGroupMapping mapping)
        {
            return mapping.Description ?? "Dynamic group created for a Lithnet Access Manager JIT request";
        }

        private string BuildGroupSamAccountName(JitDynamicGroupMapping mapping, IUser user, IGroup group)
        {
            if (string.IsNullOrWhiteSpace(mapping.GroupNameTemplate))
            {
                return $"AMS-JIT-{group.SamAccountName}-{user.SamAccountName}";
            }
            else
            {
                return mapping.GroupNameTemplate
                    .Replace("{user}", user.SamAccountName, StringComparison.OrdinalIgnoreCase)
                    .Replace("{group}", group.SamAccountName, StringComparison.OrdinalIgnoreCase)
                    .Replace("{guid}", Guid.NewGuid().ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        public string BuildGroupDomain(IGroup group)
        {
            return this.discoveryServices.GetDomainNameNetBios(group.Sid.AccountDomainSid);
        }

        private string GetDcLocatorTarget(IComputer computer)
        {
            return computer.DnsHostName ?? computer.SamAccountName.TrimEnd('$');
        }

        private DcLocatorMode GetDcLocatorMode()
        {
            if (this.options.DcLocatorMode == JitDcLocatorMode.Default)
            {
                return DcLocatorMode.RemoteDcLocator | DcLocatorMode.SiteLookup;
            }

            DcLocatorMode mode = DcLocatorMode.LocalDcLocator;

            if (this.options.DcLocatorMode.HasFlag(JitDcLocatorMode.RemoteDcLocator))
            {
                mode |= DcLocatorMode.RemoteDcLocator;
            }

            if (this.options.DcLocatorMode.HasFlag(JitDcLocatorMode.SiteLookup))
            {
                mode |= DcLocatorMode.SiteLookup;
            }

            return mode;
        }

        private JitDynamicGroupMapping FindDomainMapping(IGroup group)
        {
            SecurityIdentifier domainSid = group.Sid.AccountDomainSid;

            foreach (var items in options.DynamicGroupMappings)
            {
                if (!items.Domain.TryParseAsSid(out SecurityIdentifier sid))
                {
                    this.logger.LogError(EventIDs.JitDynamicGroupInvalidDomain, "The domain value in the JIT dynamic group mapping could not be converted to a SID: {domain}", items.Domain);
                    continue;
                }

                if (sid == domainSid)
                {
                    return items;
                }
            }

            throw new NoDynamicGroupMappingForDomainException($"The domain that contains the JIT group {group.MsDsPrincipalName} does not have the PAM feature enabled in the directory, and no matching dynamic group mapping was found. Either upgrade the forest to allow the PAM feature, or configure a dynamic group mapping in the JIT configuration");
        }
    }
}