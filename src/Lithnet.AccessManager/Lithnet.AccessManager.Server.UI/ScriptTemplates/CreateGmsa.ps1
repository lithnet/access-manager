# Create-GroupManagedServiceAccount
#
# This script enables the KDS service in the domain if it is not already enabled,
# and creates a new group-managed service account to use with the Access Manager service
#
# This script requires membership in the Domain Admins group
#
# Version 1.0

#-------------------------------------------------------------------------
# Set the following values as appropriate for your environment
#-------------------------------------------------------------------------

$accountName = "svc-lithnetams"
$description = "Service account for the Lithnet Access Manager Service"

#-------------------------------------------------------------------------
# Do not modify below here
#-------------------------------------------------------------------------

$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'
$amsServerName = "{serverName}$"

try 
{
    $ComputerExists = Get-ADComputer -Identity $amsServerName -ErrorAction Stop
} 
catch 
{
    Write-Warning "Computer account $amsServerName doesn't exists. Terminating."
    return
}

$RootKey = Get-KdsRootKey

if (-not $RootKey) 
{
    Write-Host 'In order to create a group-managed service account, a KDS key must be generated for this domain. If this is a non-production domain, you can generate a new key with immediate effect, otherwise you must wait 10 hours for the key to replicate to all DCs before proceeding.' -ForegroundColor Yellow
    Write-Host 'Visit the following site to learn more: https://docs.microsoft.com/en-us/windows-server/security/group-managed-service-accounts/create-the-key-distribution-services-kds-root-key' -ForegroundColor Yellow
    $result = Read-Host -Prompt 'Do you want to create the key with immediate effect? Y/N'

    if ($result -eq "Y")
    {
        Write-Host "Generating KDS root key with immediate effect" -ForegroundColor Green
        $key = Add-KdsRootKey -EffectiveTime ((get-date).AddHours(-10))
    }
    else
    {
        Write-Host "Generating KDS root key" -ForegroundColor Green
        $key = Add-KdsRootKey -EffectiveImmediately
    }

    Sleep 5
}

$RootKey = Get-KdsRootKey

if (-not $RootKey)
{
    Write-Warning "KDS Root Key not available. Terminating."
    return
}

$remainingTime = $RootKey.EffectiveTime.AddHours(10).Subtract((Get-Date));

if ($remainingTime -gt 0) 
{
    Write-Warning "The KDS root key is not yet available. Microsoft recommend waiting 10 hours before using the KDS key, to ensure it has had time to replicate to all domain controllers. The KDS key will be ready in $($remainingTime). Please re-run this script after that time."
    return
}

try 
{
    New-ADServiceAccount -Name $accountName -AccountNotDelegated $true -Description $description -Enabled $true -KerberosEncryptionType AES256, AES128 -SamAccountName $accountName -RestrictToOutboundAuthenticationOnly -ErrorAction Stop
}
catch
{
    Write-Warning "New-ADServiceAccount - Error creating GMSA account. Terminating with error: $($_.Exception.Message)"
    return
}

try 
{
    Set-ADServiceAccount -Identity $accountName -PrincipalsAllowedToRetrieveManagedPassword $ComputerExists.SamAccountName
}
catch 
{
    Write-Warning "Set-ADServiceAccount - Error assigning GMSA account. Terminating with error: $($_.Exception.Message)"
    return
}

$serviceAccountName = "$((Get-ADDomain).NetbiosName)\$accountName$"

Write-Host "You may need to wait for domain replicate to complete before you are able to use the account" -ForegroundColor Yellow
write-Host
Write-Host "The service account has been created. Use $ServiceAccountName in the installer." -ForegroundColor Yellow