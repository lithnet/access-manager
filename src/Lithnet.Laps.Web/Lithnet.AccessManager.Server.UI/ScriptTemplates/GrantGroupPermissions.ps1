# Grant-GroupPermissions
# 
# This script grants permissions for the AMS service account to create, delete, and manage groups in the specified OU
#
# This script requires membership in the Domain Admins group 
# 
#
# Version 1.0

#-------------------------------------------------------------------------
# Do not modify below here
#-------------------------------------------------------------------------

Import-Module ActiveDirectory

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

$domain = "{domain}"
$server = (Get-ADDomainController -DomainName $domain -Discover -ForceDiscover -Writable).HostName[0]

$rootDSE = Get-ADRootDSE -Server $server
$schemaNC = $rootDSE.schemaNamingContext
$groupObjectGuid = New-Object Guid @(,(Get-ADObject -Server $server -SearchBase $schemaNC -LdapFilter "(&(ldapDisplayName=group)(objectclass=classSchema))"-Properties "SchemaIDGuid" ).SchemaIDGuid)

$sid = new-object System.Security.Principal.SecurityIdentifier "{serviceAccount}"
$ou = "{ou}"

$ace1user = new-object System.DirectoryServices.ActiveDirectoryAccessRule $sid, "CreateChild,DeleteChild", "Allow", $groupObjectGuid, "None"
$ace2user = new-object System.DirectoryServices.ActiveDirectoryAccessRule $sid, "GenericAll", "Allow", "Children", $groupObjectGuid
 
if ((Get-PSDrive -Name AD_AMS -ErrorAction SilentlyContinue) -ne $null)
{
    Remove-PSDrive -Name AD_AMS
}

New-PSDrive -Name AD_AMS -PSProvider ActiveDirectory -Server $server -root "//RootDSE/" | out-null

$path = "AD_AMS:\$OU"

$acl = get-acl -Path $path 
$acl.AddAccessRule($ace1user)
$acl.AddAccessRule($ace2user)
set-acl -AclObject $acl -Path $path

