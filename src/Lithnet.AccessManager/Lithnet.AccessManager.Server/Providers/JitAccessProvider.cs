using System;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager
{
    public class JitAccessProvider : IJitAccessProvider
    {
        private readonly IDirectory directory;

        private readonly ILogger<JitAccessProvider> logger;

        private readonly JitConfigurationOptions options;

        public JitAccessProvider(IDirectory directory, ILogger<JitAccessProvider> logger, IOptionsSnapshot<JitConfigurationOptions> options)
        {
            this.directory = directory;
            this.logger = logger;
            this.options = options.Value;
        }

        public TimeSpan GrantJitAccess(IGroup group, IUser user, bool canExtend, TimeSpan requestedExpiry, out Action undo)
        {
            if (this.directory.IsPamFeatureEnabled(group.Sid, false))
            {
                return this.GrantJitAccessPam(group, user, canExtend, requestedExpiry, out undo);
            }
            else
            {
                return this.GrantJitAccessDynamicGroup(group, user, canExtend, requestedExpiry, out undo);
            }
        }

        public TimeSpan GrantJitAccessDynamicGroup(IGroup group, IUser user, bool canExtend, TimeSpan requestedExpiry, out Action undo)
        {
            JitDynamicGroupMapping mapping = this.FindDomainMapping(group);
            string groupName = this.BuildGroupSamAccountName(mapping, user, group);
            string description = this.BuildGroupDescription(mapping);
            string fqGroupName = $"{this.BuildGroupDomain(group)}\\{groupName}";

            TimeSpan grantedExpiry = requestedExpiry;

            this.logger.LogTrace("Processing request to have {user} added to the JIT group {group} via dynamicObject {dynamicGroup}", user.MsDsPrincipalName, group.MsDsPrincipalName, fqGroupName);

            if (directory.TryGetGroup(fqGroupName, out IGroup dynamicGroup))
            {
                this.logger.LogTrace("Dynamic group {dynamicGroup} already exists in the directory with a remaining ttl of {ttl}", dynamicGroup.MsDsPrincipalName, dynamicGroup.EntryTtl);

                if (!canExtend)
                {
                    this.logger.LogTrace("User {user} is not permitted to extend the access, so the ttl will remain unchanged", user.MsDsPrincipalName);
                    grantedExpiry = dynamicGroup.EntryTtl ?? new TimeSpan();
                }
                else
                {
                    dynamicGroup.ExtendTtl(requestedExpiry);
                    this.logger.LogTrace("User {user} is permitted to extend the access, so the ttl will was updated to {ttl}", user.MsDsPrincipalName, requestedExpiry);
                }
            }
            else
            {
                this.logger.LogTrace("Creating a new dynamic group {groupName} in {ou} with ttl of {ttl}", groupName, mapping.GroupOU, grantedExpiry);
                dynamicGroup = this.directory.CreateTtlGroup(groupName, groupName, description, mapping.GroupOU, grantedExpiry, true);
                this.logger.LogInformation(EventIDs.JitDynamicGroupCreated, "Created a new dynamic group {groupName} in {ou} with ttl of {ttl}", groupName, mapping.GroupOU, grantedExpiry);
            }

            this.logger.LogTrace("Adding user {user} to dynamic group {dynamicGroup}", user.MsDsPrincipalName, dynamicGroup.MsDsPrincipalName);
            dynamicGroup.AddMember(user);

            this.logger.LogTrace("Adding dynamic group {dynamicGroup} to the JIT group {jitGroup}", dynamicGroup.MsDsPrincipalName, group.MsDsPrincipalName);
            group.AddMember(dynamicGroup);

            undo = () =>
            {
                this.logger.LogTrace("Rolling back JIT access by deleting dynamic group {dynamicGroup} created for {user} to become a member of {group}", dynamicGroup.MsDsPrincipalName, user.MsDsPrincipalName, group.MsDsPrincipalName);
                this.directory.DeleteGroup(fqGroupName);
                this.logger.LogInformation(EventIDs.JitDynamicGroupDeleted, "Rolled back JIT access by deleting dynamic group {dynamicGroup} created for {user} to become a member of {group}", dynamicGroup.MsDsPrincipalName, user.MsDsPrincipalName, group.MsDsPrincipalName);
            };

            return grantedExpiry;
        }

        public TimeSpan GrantJitAccessPam(IGroup group, IUser user, bool canExtend, TimeSpan requestedExpiry, out Action undo)
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

            group.AddMember(user, requestedExpiry);
            this.logger.LogInformation(EventIDs.JitPamAccessGranted, "User {user} was added to group {group} with a membership expiry of {ttl}", user.MsDsPrincipalName, group.MsDsPrincipalName, requestedExpiry);

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
                return $"LITHNET-AMS-JIT-{group.SamAccountName}-{user.SamAccountName}";
            }
            else
            {
                return mapping.GroupNameTemplate
                    .Replace("{user}", user.SamAccountName, StringComparison.OrdinalIgnoreCase)
                    .Replace("{group}", group.SamAccountName, StringComparison.OrdinalIgnoreCase);
            }
        }

        public string BuildGroupDomain(IGroup group)
        {
            return directory.GetDomainNameNetBiosFromSid(group.Sid.AccountDomainSid);
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