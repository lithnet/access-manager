# Grant-AccessManagerPermissions
# 
# This script grants permissions for computer objects to write their encrypted password details to the directory, and allows the Lithnet Access Manager service account to read that data
#
# This script requires membership in the Domain Admins group 
#
# Version 1.0


#-------------------------------------------------------------------------
# Set the following values as appropriate for your environment
#-------------------------------------------------------------------------

# Leave this value blank to apply the permissions at the top level of the domain where this script is being run
# Otherwise, specify the OU where permissions should be delegated
$OU = "" 

#-------------------------------------------------------------------------
# Do not modify below here
#-------------------------------------------------------------------------

Import-Module ActiveDirectory

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

$rootDSE = Get-ADRootDSE

if ([string]::IsNullOrEmpty($ou))
{
	$ou = $rootDSE.defaultNamingContext
}

$schemaNC = $rootDSE.schemaNamingContext
$computerObjectGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=computer)(objectclass=classSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)
$lithnetAdminPasswordGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=lithnetAdminPassword)(objectclass=attributeSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)
$lithnetAdminPasswordExpiryGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=lithnetAdminPasswordExpiry)(objectclass=attributeSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)
$lithnetAdminPasswordHistoryGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=lithnetAdminPasswordHistory)(objectclass=attributeSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)

$selfSid = new-object System.Security.Principal.SecurityIdentifier "S-1-5-10"
$serviceAccountSid = new-object System.Security.Principal.SecurityIdentifier "{serviceAccount}"

$propertyAccessRule1 = new-object "System.DirectoryServices.PropertyAccessRule" $serviceAccountSid, "Allow", "Read", $lithnetAdminPasswordGuid, "Descendents", $computerObjectGuid
$propertyAccessRule2 = new-object "System.DirectoryServices.PropertyAccessRule" $serviceAccountSid, "Allow", "Read", $lithnetAdminPasswordExpiryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule3 = new-object "System.DirectoryServices.PropertyAccessRule" $serviceAccountSid, "Allow", "Read", $lithnetAdminPasswordHistoryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule4 = new-object "System.DirectoryServices.PropertyAccessRule" $serviceAccountSid, "Allow", "Write", $lithnetAdminPasswordExpiryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule5 = new-object "System.DirectoryServices.ActiveDirectoryAccessRule" $serviceAccountSid, "ExtendedRight", "Allow", $lithnetAdminPasswordGuid, "Descendents", $computerObjectGuid
$propertyAccessRule6 = new-object "System.DirectoryServices.ActiveDirectoryAccessRule" $serviceAccountSid, "ExtendedRight", "Allow", $lithnetAdminPasswordHistoryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule7 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Read", $lithnetAdminPasswordGuid, "Descendents", $computerObjectGuid
$propertyAccessRule8 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Read", $lithnetAdminPasswordExpiryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule9 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Read", $lithnetAdminPasswordHistoryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule10 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Write", $lithnetAdminPasswordGuid, "Descendents", $computerObjectGuid
$propertyAccessRule11 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Write", $lithnetAdminPasswordExpiryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule12 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Write", $lithnetAdminPasswordHistoryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule13 = new-object "System.DirectoryServices.ActiveDirectoryAccessRule" $selfSid, "ExtendedRight", "Allow", $lithnetAdminPasswordGuid, "Descendents", $computerObjectGuid
$propertyAccessRule14 = new-object "System.DirectoryServices.ActiveDirectoryAccessRule" $selfSid, "ExtendedRight", "Allow", $lithnetAdminPasswordHistoryGuid, "Descendents", $computerObjectGuid


$path = "AD:\$OU"

$acl = get-acl -Path $path
$acl.AddAccessRule($propertyAccessRule1)
$acl.AddAccessRule($propertyAccessRule2)
$acl.AddAccessRule($propertyAccessRule3)
$acl.AddAccessRule($propertyAccessRule4)
$acl.AddAccessRule($propertyAccessRule5)
$acl.AddAccessRule($propertyAccessRule6)
$acl.AddAccessRule($propertyAccessRule7)
$acl.AddAccessRule($propertyAccessRule8)
$acl.AddAccessRule($propertyAccessRule9)
$acl.AddAccessRule($propertyAccessRule10)
$acl.AddAccessRule($propertyAccessRule11)
$acl.AddAccessRule($propertyAccessRule12)
$acl.AddAccessRule($propertyAccessRule13)
$acl.AddAccessRule($propertyAccessRule14)
set-acl -AclObject $acl -Path $path