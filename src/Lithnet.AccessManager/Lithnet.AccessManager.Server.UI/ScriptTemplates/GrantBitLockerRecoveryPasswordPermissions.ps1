# Grant-BitLockerRecoveryPasswordPermissions
# 
# This script grants permissions that allow the Lithnet Admin Access Manager service account to read BitLocker recovery passwords from Active Directory
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
$fveRecoveryObjectGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=msFVE-RecoveryInformation)(objectclass=classSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)
$fveRecoveryPasswordGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=msFVE-RecoveryPassword)(objectclass=attributeSchema)(!(isDefunct=true)))"-Properties "SchemaIDGuid" ).SchemaIDGuid)

$serviceAccountSid = new-object System.Security.Principal.SecurityIdentifier "{serviceAccount}"

$propertyAccessRule1 = new-object "System.DirectoryServices.ActiveDirectoryAccessRule" $serviceAccountSid, "ReadProperty", "Allow", "Descendents", $fveRecoveryObjectGuid
$propertyAccessRule4 = new-object "System.DirectoryServices.ActiveDirectoryAccessRule" $serviceAccountSid, "ExtendedRight", "Allow", $fveRecoveryPasswordGuid, "Descendents", $fveRecoveryObjectGuid

$path = "AD:\$OU"

$acl = get-acl -Path $path
$acl.AddAccessRule($propertyAccessRule1)
$acl.AddAccessRule($propertyAccessRule2)
$acl.AddAccessRule($propertyAccessRule3)
$acl.AddAccessRule($propertyAccessRule4)

set-acl -AclObject $acl -Path $path