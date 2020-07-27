# Enable-PamFeature
# 
# This script enabled the 'Privileged Access Management' optional feature in a forest
#
# This script requires membership in the Domain Admins group of the forest root domain or Enterprise Admins
#
# Version 1.0

#-------------------------------------------------------------------------
# Do not modify below here
#-------------------------------------------------------------------------

Import-Module ActiveDirectory

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

$domain = "{domain}"

Get-ADOptionalFeature 'Privileged Access Management Feature' -Server $domain | Enable-ADOptionalFeature -Scope ForestOrConfigurationSet -Target $domain
