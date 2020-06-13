function Get-LapsAuthorizationResponse{
	param(
	[Lithnet.Laps.Web.ActiveDirectory.IUser]$user,
	[Lithnet.Laps.Web.ActiveDirectory.IComputer]$computer,
	[Nlog.ILogger]$logger
)

	$logger.Trace("We're in PowerShell!");
	$logger.Trace("Checking if $($user.SamAccountName) has access to LAPS for $($computer.SamAccountName)");

	$response = New-Object -TypeName "Lithnet.Laps.Web.Authorization.LapsAuthorizationResponse"

	$response.Code = "ExplicitlyDenied";
	$response.MatchedRuleDescription = "nah mate";

	$logger.Trace($response.ResponseCode);


	Write-Output $response;
}

function Get-JitAuthorizationResponse{
	param(
	[Lithnet.Laps.Web.ActiveDirectory.IUser]$user,
	[Lithnet.Laps.Web.ActiveDirectory.IComputer]$computer,
	[Nlog.ILogger]$logger
)

	$logger.Trace("We're in PowerShell!");
	$logger.Trace("Checking if $($user.SamAccountName) can request JIT access to $($computer.SamAccountName)");

	$response = New-Object -TypeName "Lithnet.Laps.Web.Authorization.JitAuthorizationResponse"

	$response.Code = "ExplicitlyDenied";
	$response.MatchedRuleDescription = "no way";
	$response.AuthorizingGroupName = $null;

	$logger.Trace($response.ResponseCode);

	Write-Output $response;
}