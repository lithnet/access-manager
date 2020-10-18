function Write-AuditLog{
    param(
    [hashtable]$tokens,
    [bool]$isSuccess
)
    Write-Information "We're in PowerShell for auditing!";

    $user = $tokens["{user.MsDsPrincipalName}"]
    $computer = $tokens["{computer.msdsprincipalname}"]
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