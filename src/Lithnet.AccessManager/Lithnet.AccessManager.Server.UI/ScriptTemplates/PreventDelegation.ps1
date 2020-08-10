# Prevent-Delegation
# 
# This script modifies the userAccountControl attribute to set the flag that prevents delegation of the specified account
#
# This script requires membership in the domain admins group, or delegated permission to manage user objects in the container where the account resides
# 
#
# Version 1.0

#-------------------------------------------------------------------------
# Do not modify below here
#-------------------------------------------------------------------------

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

Import-Module ActiveDirectory
    
$sid = "{sid}"            
Set-ADAccountControl -Identity $sid -AccountNotDelegated:$true     
            
