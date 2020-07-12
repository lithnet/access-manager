function Get-LapsAuthorizationResponse{
	param(
	[Lithnet.AccessManager.IUser]$user,
	[Lithnet.AccessManager.IComputer]$computer,
	[Nlog.ILogger]$logger
)

	$logger.Trace("We're in PowerShell!");
	$logger.Trace("Checking if $($user.MsDsPrincipalName) has access to LAPS for $($computer.MsDsPrincipalName)");

	$response = New-Object -TypeName "Lithnet.AccessManager.Server.Authorization.PowerShellAuthorizationResponse"

	# Set IsAllowed to true to allow access, or set IsDenied to explicitly deny access, or leave both as false if no decision was made. This will allow other rules to be evaluated.
	$response.IsAllowed = $false;
	$response.IsDenied = $false;

	Write-Output $response;
}

function Get-LapsHistoryAuthorizationResponse{
	param(
	[Lithnet.AccessManager.IUser]$user,
	[Lithnet.AccessManager.IComputer]$computer,
	[Nlog.ILogger]$logger
)

	$logger.Trace("We're in PowerShell!");
	$logger.Trace("Checking if $($user.MsDsPrincipalName) has access to LAPS password history for $($computer.MsDsPrincipalName)");

	$response = New-Object -TypeName "Lithnet.AccessManager.Server.Authorization.PowerShellAuthorizationResponse"
		
	# Set IsAllowed to true to allow access, or set IsDenied to explicitly deny access, or leave both as false if no decision was made. This will allow other rules to be evaluated.
	$response.IsAllowed = $false;
	$response.IsDenied = $false;

	Write-Output $response;
}


function Get-JitAuthorizationResponse{
	param(
	[Lithnet.AccessManager.IUser]$user,
	[Lithnet.AccessManager.IComputer]$computer,
	[Nlog.ILogger]$logger
)

	$logger.Trace("We're in PowerShell!");
	$logger.Trace("Checking if $($user.MsDsPrincipalName) can request JIT access to $($computer.MsDsPrincipalName)");

	$response = New-Object -TypeName "Lithnet.AccessManager.Server.Authorization.PowerShellAuthorizationResponse"
	
	# Set IsAllowed to true to allow access, or set IsDenied to explicitly deny access, or leave both as false if no decision was made. This will allow other rules to be evaluated.
	$response.IsAllowed = $false;
	$response.IsDenied = $false;

	Write-Output $response;
}