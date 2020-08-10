# Add-DomainGroupMembershipPermissions
# 
# This script adds the Access Manager service account to the "Windows Authorization Access Group" and "Access Control Assistance Operators" groups in the specified domain
#
# This script requires membership in the Domain Admins group for the domain where permissions need to be added
#
# Version 1.0


#-------------------------------------------------------------------------
# Do not modify below here
#-------------------------------------------------------------------------

$domain = "{domainDns}"
$serviceAccountSid = "{serviceAccountSid}"

# Get the Windows Authorization Access Group by it's well-known SID "S-1-5-32-560"
$de = new-object "System.DirectoryServices.DirectoryEntry" "LDAP://$domain/<SID=S-1-5-32-560>"
$de.Properties["member"].Add("<SID=$serviceAccountSid>") ## Add our service account as a member
$de.CommitChanges()

# Get the Access Control Assistance Operators group by it's well-known SID "S-1-5-32-560"
$de = new-object "System.DirectoryServices.DirectoryEntry" "LDAP://$domain/<SID=S-1-5-32-579>"
$de.Properties["member"].Add("<SID=$serviceAccountSid>") ## Add our service account as a member
$de.CommitChanges()

