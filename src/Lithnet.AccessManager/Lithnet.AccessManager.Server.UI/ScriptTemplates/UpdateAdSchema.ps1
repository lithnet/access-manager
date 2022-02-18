# Update-AdSchema
# 
# This script creates the attributes and object classes required to enable encrypted local admin passwords and password history support
# with the Lithnet Access Manager Agent
#
# This script requires membership in their the Schema Admin group
# 
#
# Version 1.0

#-------------------------------------------------------------------------
# Do not modify below here
#-------------------------------------------------------------------------

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

Import-Module ActiveDirectory

$forest = "{forest}"
$server = (Get-ADDomainController -DomainName $forest -Discover -ForceDiscover -Writable).HostName[0]
$rootDSE = Get-ADRootDSE -Server $server
$schemaNC = $rootDSE.schemaNamingContext
$schemaMaster = Get-ADObject $schemaNC -Properties fSMORoleOwner -server $server | Get-ADDomainController -Identity { $_.fSMORoleOwner } -Server $server
$schemaMasterRootDse = [ADSI]::new("LDAP://$($schemaMaster.HostName)/RootDSE")

if (-not (Get-ADObject -SearchBase $schemaNC -Server $schemaMaster.HostName -LdapFilter "(&(ldapDisplayName=lithnetAdminPassword)(objectclass=attributeSchema)(!(isDefunct=TRUE)))"))
{
    New-ADObject -Name "Lithnet-Admin-Password" -Type "attributeSchema" -Path $schemaNC -Server $schemaMaster.HostName -OtherAttributes @{
        schemaIDGUID                  = [Guid]::new("f440de15-5e53-4522-a221-0000b9d757dd").ToByteArray()
        attributeID                   = "1.3.6.1.4.1.55989.1.1.1"
        lDAPDisplayName               = "lithnetAdminPassword"
        adminDisplayName              = "lithnetAdminPassword"
        attributeSyntax               = "2.5.5.12"
        oMSyntax                      = 64
        searchFlags                   = 904
        isSingleValued                = $true
        isMemberOfPartialAttributeSet = $false
        systemOnly                    = $false
        showInAdvancedViewOnly        = $false
    } 

    Write-Host "The attribute lithnetAdminPassword was created"
}
else
{
    Write-Host "The attribute lithnetAdminPassword already exists"
}

if (-not (Get-ADObject -SearchBase $schemaNC -Server $schemaMaster.HostName -LdapFilter "(&(ldapDisplayName=lithnetAdminPasswordHistory)(objectclass=attributeSchema)(!(isDefunct=TRUE)))"))
{
    New-ADObject -Name "Lithnet-Admin-Password-History" -Type "attributeSchema" -Path $schemaNC -Server $schemaMaster.HostName -OtherAttributes @{
        schemaIDGUID                  = [Guid]::new("5e7f84e2-9561-4ac3-b3b4-0000121aa663").ToByteArray()
        attributeID                   = "1.3.6.1.4.1.55989.1.1.2"
        lDAPDisplayName               = "lithnetAdminPasswordHistory"
        adminDisplayName              = "lithnetAdminPasswordHistory"
        attributeSyntax               = "2.5.5.12"
        oMSyntax                      = 64
        searchFlags                   = 904
        isSingleValued                = $false
        isMemberOfPartialAttributeSet = $false
        systemOnly                    = $false
        showInAdvancedViewOnly        = $false
    }

    Write-Host "The attribute lithnetAdminPasswordHistory was created"
}
else
{
    Write-Host "The attribute lithnetAdminPasswordHistory already exists"
}

if (-not (Get-ADObject -SearchBase $schemaNC -Server $schemaMaster.HostName -LdapFilter "(&(ldapDisplayName=lithnetAdminPasswordExpiry)(objectclass=attributeSchema)(!(isDefunct=TRUE)))"))
{
    New-ADObject -Name "Lithnet-Admin-Password-Expiry" -Type "attributeSchema" -Path $schemaNC -Server $schemaMaster.HostName -OtherAttributes @{
        schemaIDGUID                  = [Guid]::new("0f65f007-22e9-4a4f-9fba-000025aa156d").ToByteArray()
        attributeID                   = "1.3.6.1.4.1.55989.1.1.3"
        lDAPDisplayName               = "lithnetAdminPasswordExpiry"
        adminDisplayName              = "lithnetAdminPasswordExpiry"
        attributeSyntax               = "2.5.5.16"
        oMSyntax                      = 65
        searchFlags                   = 0
        isSingleValued                = $true
        isMemberOfPartialAttributeSet = $false
        systemOnly                    = $false
        showInAdvancedViewOnly        = $false
    }

    Write-Host "The attribute lithnetAdminPasswordExpiry was created"
}
else
{
    Write-Host "The attribute lithnetAdminPasswordExpiry already exists"
}

$schemaMasterRootDse.Put("schemaUpdateNow", 1)
$schemaMasterRootDse.SetInfo()

$computerClass = Get-ADObject -SearchBase $schemaNC -Server $schemaMaster.HostName -LdapFilter "(&(ldapDisplayName=computer)(objectclass=classSchema))" -Properties *
if (-not $computerClass.mayContain.ValueList.Contains("lithnetAdminPassword"))
{
     $computerClass | Set-ADObject -Server $schemaMaster.HostName -Add @{ mayContain = @("lithnetAdminPassword") }
     Write-Host "The attribute lithnetAdminPassword was added to the computer class"
}
else
{
     Write-Host "The attribute lithnetAdminPassword was already present on the computer class"
}


if (-not $computerClass.mayContain.ValueList.Contains("lithnetAdminPasswordHistory"))
{
     $computerClass | Set-ADObject -Server $schemaMaster.HostName -Add @{ mayContain = @("lithnetAdminPasswordHistory") }
     Write-Host "The attribute lithnetAdminPasswordHistory was added to the computer class"
}
else
{
     Write-Host "The attribute lithnetAdminPasswordHistory was already present on the computer class"
}


if (-not $computerClass.mayContain.ValueList.Contains("lithnetAdminPasswordExpiry"))
{
     $computerClass | Set-ADObject -Server $schemaMaster.HostName -Add @{ mayContain = @("lithnetAdminPasswordExpiry") }
     Write-Host "The attribute lithnetAdminPasswordExpiry was added to the computer class"
}
else
{
     Write-Host "The attribute lithnetAdminPasswordExpiry was already present on the computer class"
}

$schemaMasterRootDse.Put("schemaUpdateNow", 1)
$schemaMasterRootDse.SetInfo()

if (-not (Get-ADObject -SearchBase $schemaNC -Server $schemaMaster.HostName -LdapFilter "(&(ldapDisplayName=lithnetAccessManagerConfig)(objectclass=classSchema))"))
{
    New-ADObject -Name "Lithnet-Access-Manager-Configuration" -Type "classSchema" -Path $schemaNC -Server $schemaMaster.HostName -OtherAttributes @{
        schemaIDGUID                  = [Guid]::new("2c6a6a6a-6f3f-407e-81c7-b7807b369368").ToByteArray()
        governsID                     = "1.3.6.1.4.1.55989.1.2.1"
        lDAPDisplayName               = "lithnetAccessManagerConfig"
        adminDisplayName              = "lithnetAccessManagerConfig"
        objectClassCategory           = 1
        subclassOf                    = "applicationSettings"
        rdnAttId                      = "cn"
        possSuperiors                 = "container"
        mayContain                    = @("cn", "keywords", "caCertificate", "msDS-ByteArray", "msDS-DateTime", "msDS-Settings", "msDS-Integer", "msDS-ObjectReference", "appSchemaVersion")
    }

    Write-Host "The object class lithnetAccessManagerConfig was created"
}
else
{
    Write-Host "The object class lithnetAccessManagerConfig already exists"
}

$schemaMasterRootDse.Put("schemaUpdateNow", 1)
$schemaMasterRootDse.SetInfo()
