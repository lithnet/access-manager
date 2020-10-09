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

$OU = ""

#-------------------------------------------------------------------------
# Do not modify below here
#-------------------------------------------------------------------------

Import-Module ActiveDirectory

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

$rootDSE = Get-ADRootDSE
$schemaNC = $rootDSE.schemaNamingContext
$computerObjectGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=computer)(objectclass=classSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)
$msLapsPasswordGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=ms-Mcs-AdmPwd)(objectclass=attributeSchema)(!(isDefunct=TRUE)))"-Properties "SchemaIDGuid" ).SchemaIDGuid)
$msLapsPasswordExpiryGuid = New-Object Guid @(,(Get-ADObject -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=ms-Mcs-AdmPwdExpirationTime)(objectclass=attributeSchema)(!(isDefunct=TRUE)))"-Properties "SchemaIDGuid" ).SchemaIDGuid)


$selfSid = new-object System.Security.Principal.SecurityIdentifier "S-1-5-10"

$propertyAccessRule1 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Write", $msLapsPasswordGuid, "Descendents", $computerObjectGuid
$propertyAccessRule2 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Write", $msLapsPasswordExpiryGuid, "Descendents", $computerObjectGuid
$propertyAccessRule3 = new-object "System.DirectoryServices.PropertyAccessRule" $selfSid, "Allow", "Read", $msLapsPasswordExpiryGuid, "Descendents", $computerObjectGuid

$path = "AD:\$OU"

$acl = get-acl -Path $path
$acl.AddAccessRule($propertyAccessRule1)
$acl.AddAccessRule($propertyAccessRule2)
$acl.AddAccessRule($propertyAccessRule3)
set-acl -AclObject $acl -Path $path
