# Grant-ComputerSelfPermission
# 
# This script grants permissions for computer objects to set their own password attributes
#
# This script requires membership in the Domain Admins group 
# 
#
# Version 1.0


#-------------------------------------------------------------------------
# Set the following values as appropriate for your environment
#-------------------------------------------------------------------------

$OU = "OU=Laps Testing,DC=IDMDEV1,DC=LOCAL"

#-------------------------------------------------------------------------
# Do not modify below here

Import-Module ActiveDirectory

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

$rootDSE = Get-ADRootDSE
$schemaNC = $rootDSE.schemaNamingContext
$computerObjectGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=computer)(objectclass=classSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)
$lithnetAdminPasswordGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=lithnetAdminPassword)(objectclass=attributeSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)
$lithnetAdminPasswordExpiryGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=lithnetAdminPasswordExpiry)(objectclass=attributeSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)
$lithnetAdminPasswordHistoryGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=lithnetAdminPasswordHistory)(objectclass=attributeSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)

$selfSid = new-object System.Security.Principal.SecurityIdentifier "S-1-5-10"

$propertyAccessRule1 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Read", $lithnetAdminPasswordGuid, "Descendents", $computerObjectGuid
$propertyAccessRule2 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Read", $lithnetAdminPasswordExpiryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule3 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Read", $lithnetAdminPasswordHistoryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule4 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Write", $lithnetAdminPasswordGuid, "Descendents", $computerObjectGuid
$propertyAccessRule5 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Write", $lithnetAdminPasswordExpiryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule6 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Write", $lithnetAdminPasswordHistoryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule7 = new-object "System.DirectoryServices.ActiveDirectoryAccessRule" $selfSid, "ExtendedRight", "Allow", $lithnetAdminPasswordGuid, "Descendents", $computerObjectGuid
$propertyAccessRule8 = new-object "System.DirectoryServices.ActiveDirectoryAccessRule" $selfSid, "ExtendedRight", "Allow", $lithnetAdminPasswordHistoryGuid, "Descendents", $computerObjectGuid


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
set-acl -AclObject $acl -Path $path