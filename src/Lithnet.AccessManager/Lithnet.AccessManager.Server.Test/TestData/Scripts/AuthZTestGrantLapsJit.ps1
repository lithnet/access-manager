function Get-AuthorizationResponse{
	param(
	[Lithnet.AccessManager.IUser]$user,
	[Lithnet.AccessManager.IComputer]$computer,
	[Nlog.ILogger]$logger
)

	$logger.Trace("We're in PowerShell!");
	$logger.Trace("Checking if $($user.MsDsPrincipalName) is allowed administrative access to $($computer.MsDsPrincipalName)");

	$response = New-Object -TypeName "Lithnet.AccessManager.Server.Authorization.PowerShellAuthorizationResponse"

	# Set IsAllowed to true to allow access, or set IsDenied to explicitly deny access, or leave both as false if no decision was made. This will allow other rules to be evaluated.
	$response.IsLocalAdminPasswordAllowed = $true;
	$response.IsLocalAdminPasswordDenied = $false;
	
	$response.IsLocalAdminPasswordHistoryAllowed = $false;
	$response.IsLocalAdminPasswordHistoryDenied = $false;
	
	$response.IsJitAllowed = $true;
	$response.IsJitDenied = $false;

	Write-Output $response;
}