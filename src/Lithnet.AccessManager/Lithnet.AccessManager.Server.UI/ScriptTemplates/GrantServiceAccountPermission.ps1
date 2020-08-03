# Grant-ServiceAccountPermission
# 
# This script grants permissions for a user or group to read the admin password values, as well as trigger a password reset by cleaning the password expiry field
#
# This script requires membership in the Domain Admins group 
# 
#
# Version 1.0

#-------------------------------------------------------------------------
# Do not modify below here
#-------------------------------------------------------------------------

$OU = "{ou}"
$userOrGroup = "{serviceAccount}"

Import-Module ActiveDirectory

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

$rootDSE = Get-ADRootDSE
$schemaNC = $rootDSE.schemaNamingContext
$groupObjectGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=group)(objectclass=classSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)

$objUser = New-Object System.Security.Principal.NTAccount($userOrGroup)
$sid = $objUser.Translate([System.Security.Principal.SecurityIdentifier])

$ace1user = new-object System.DirectoryServices.ActiveDirectoryAccessRule $sid, "CreateChild", "Allow", "None", $groupObjectGuid
$ace2user = new-object System.DirectoryServices.ActiveDirectoryAccessRule $sid, "DeleteChild", "Allow", "None", $groupObjectGuid
$ace3user = new-object System.DirectoryServices.ActiveDirectoryAccessRule $sid, "GenericAll", "Allow", "Children", $groupObjectGuid
 
$path = "AD:\$OU"

$acl = get-acl -Path $path
$acl.AddAccessRule($ace1user)
$acl.AddAccessRule($ace2user)
$acl.AddAccessRule($ace3user)
set-acl -AclObject $acl -Path $path