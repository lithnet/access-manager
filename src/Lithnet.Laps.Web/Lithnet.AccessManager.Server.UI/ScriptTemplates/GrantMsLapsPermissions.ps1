# Grant-MsLapsPermissions
# 
# This script grants permissions that allow the Lithnet Admin Access Manager service account to read the Microsoft LAPS passwords from Active Directory
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
$msLapsPasswordGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=ms-Mcs-AdmPwd)(objectclass=attributeSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)
$msLapsPasswordExpiryGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=ms-Mcs-AdmPwdExpirationTime)(objectclass=attributeSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)

$serviceAccountSid = new-object System.Security.Principal.SecurityIdentifier "{serviceAccount}"

$propertyAccessRule1 = new-object "System.DirectoryServices.PropertyAccessRule" $serviceAccountSid, "Allow", "Read", $msLapsPasswordGuid, "Descendents", $computerObjectGuid
$propertyAccessRule2 = new-object "System.DirectoryServices.PropertyAccessRule" $serviceAccountSid, "Allow", "Read", $msLapsPasswordExpiryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule3 = new-object "System.DirectoryServices.PropertyAccessRule" $serviceAccountSid, "Allow", "Write", $msLapsPasswordExpiryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule4 = new-object "System.DirectoryServices.ActiveDirectoryAccessRule" $serviceAccountSid, "ExtendedRight", "Allow", $msLapsPasswordGuid, "Descendents", $computerObjectGuid

$path = "AD:\$OU"

$acl = get-acl -Path $path
$acl.AddAccessRule($propertyAccessRule1)
$acl.AddAccessRule($propertyAccessRule2)
$acl.AddAccessRule($propertyAccessRule3)
$acl.AddAccessRule($propertyAccessRule4)

set-acl -AclObject $acl -Path $path