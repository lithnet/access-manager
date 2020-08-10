# Create-GroupManagedServiceAccount
# 
# This script enables the KDS service in the domain if it is not already enabled, and creates a new group-managed service account to use with the Access Manager service
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

$amsServerName = "{serverName}$"

$kdsCreated = $false;

if ((Get-KdsRootKey )-eq $null)
{
    Add-KdsRootKey -EffectiveImmediately
    $kdsCreated=$true;
}

New-ADServiceAccount -Name $accountName -AccountNotDelegated:$true -Description $description -Enabled:$true -KerberosEncryptionType  "AES256,AES128" -SamAccountName $accountName -RestrictToOutboundAuthenticationOnly
Set-ADServiceAccount -Identity $accountName -PrincipalsAllowedToRetrieveManagedPassword $amsServerName

$serviceAccountName = "$((Get-ADDomain).NetbiosName)\$accountName$"

if ($kdsCreated)
{
    Write-Host "The KDS has been enabled in the domain and the service account has been created" -ForegroundColor Yellow
    Write-Host "You may need to wait up to 10 hours for all domain controllers in the domain to replicate the key before you are able to use the account" -ForegroundColor Yellow
    Write-Host "After this time, use the Access Manager configuration tool to change the service account name to $serviceAccountName and leave the password field blank" -ForegroundColor Yellow
}
else
{
    Write-Host "The service account has been created." -ForegroundColor Yellow
    Write-Host "Use the Access Manager configuration tool to change the service account name to $serviceAccountName and leave the password field blank" -ForegroundColor Yellow
}
