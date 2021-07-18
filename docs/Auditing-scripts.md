## Customized auditing with PowerShell notification channels
![](images/ui-page-auditing-powershell-channel.png)

Access Manager's auditing framework allows you to extend the product's auditing capabilities through the use of PowerShell scripts. Using a PowerShell notification channel, you can send audit events to any system you can connect to with PowerShell.

## Example script 

The following script is a very simple example that extracts some audit information from the supplied hashtable, and writes to the PowerShell information stream.

```ps
function Write-AuditLog{
    param(
    [hashtable]$tokens,
    [bool]$isSuccess
)
    Write-Information "We're in PowerShell for auditing!";

    $user = $tokens["{user.MsDsPrincipalName}"]
    $computer = $tokens["{computer.MsDsPrincipalName}"]
    $result = $tokens["{AuthzResult.ResponseCode}"]
    $accessType = $tokens["{AuthzResult.AccessType}"]

    if ($isSuccess)
    {
        Write-Information "User $user successfully requested $accessType access to $computer";
    }
    else
    {
        Write-Information "User $user was denied $accessType access to $computer with response code $result";
    }
}

```

## Parameters
### $tokens
The `$tokens` variable is a hashtable of keys and values that contain information about the access request. See the `Available variables` section below for the list of available variables you can use.

### $isSuccess
A boolean value that indicates if this audit event represents a successful access granted event or a failure.

## Logging information
You can use the Write-Information, Write-Warning, Write-Verbose cmdlets to write to the AMS log file. Note that if you use Write-Error, or if an exception is thrown and not handled, the auditing event will fail. If you tick the `Deny the user's request if the delivery of this notification fails` option, the user's request will be denied.

## Performance
If you use the `Deny the user's request if the delivery of this notification fails`, the user's access request cannot be granted until your script completes.  From the user's perspective, they will be waiting in the browser while the audit event takes place. If you have a slow script, or lots of scripts, this wait time may seem excessive.

If you do not use this option, the user is granted their access request, and the audit event is delivered asynchronously. Any failures will be logged in the AMS logs, but the user will not be denied access.

## Available variables
The [[audit variables]] page contains a full list of all valid variables that you can use throughout your scripts.