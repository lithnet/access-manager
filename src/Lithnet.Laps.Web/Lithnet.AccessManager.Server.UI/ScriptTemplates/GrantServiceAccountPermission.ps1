# Grant-ServiceAccountPermission
# 
# This script grants permissions for a user or group to read the admin password values, as well as trigger a password reset by cleaning the password expiry field
#
# This script requires membership in the Domain Admins group 
# 
#
# Version 1.0


#-------------------------------------------------------------------------
# Set the following values as appropriate for your environment
#-------------------------------------------------------------------------

$OU = "OU=Laps Testing,DC=IDMDEV1,DC=LOCAL"
$userOrGroup = "IDMDEV1\Domain Admins"

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

$objUser = New-Object System.Security.Principal.NTAccount($userOrGroup)
$sid = $objUser.Translate([System.Security.Principal.SecurityIdentifier])

$propertyAccessRule1 = new-object "System.DirectoryServices.PropertyAccessRule" $sid, "Allow", "Read", $lithnetAdminPasswordGuid, "Descendents", $computerObjectGuid
$propertyAccessRule2 = new-object "System.DirectoryServices.PropertyAccessRule" $sid, "Allow", "Read", $lithnetAdminPasswordExpiryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule3 = new-object "System.DirectoryServices.PropertyAccessRule" $sid, "Allow", "Read", $lithnetAdminPasswordHistoryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule4 = new-object "System.DirectoryServices.PropertyAccessRule" $sid, "Allow", "Write", $lithnetAdminPasswordExpiryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule5 = new-object "System.DirectoryServices.ActiveDirectoryAccessRule" $sid, "ExtendedRight", "Allow", $lithnetAdminPasswordGuid, "Descendents", $computerObjectGuid
$propertyAccessRule6 = new-object "System.DirectoryServices.ActiveDirectoryAccessRule" $sid, "ExtendedRight", "Allow", $lithnetAdminPasswordHistoryGuid, "Descendents", $computerObjectGuid

$path = "AD:\$OU"

$acl = get-acl -Path $path
$acl.AddAccessRule($propertyAccessRule1)
$acl.AddAccessRule($propertyAccessRule2)
$acl.AddAccessRule($propertyAccessRule3)
$acl.AddAccessRule($propertyAccessRule4)
$acl.AddAccessRule($propertyAccessRule5)
$acl.AddAccessRule($propertyAccessRule6)
set-acl -AclObject $acl -Path $path