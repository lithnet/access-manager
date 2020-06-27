function Get-LapsAuthorizationResponse{
	param(
	[Lithnet.AccessManager.IUser]$user,
	[Lithnet.AccessManager.IComputer]$computer,
	[Nlog.ILogger]$logger
)

	$logger.Trace("We're in PowerShell!");
	$logger.Trace("Checking if $($user.MsDsPrincipalName) has access to LAPS for $($computer.MsDsPrincipalName)");

	$response = New-Object -TypeName "Lithnet.AccessManager.Web.Authorization.LapsAuthorizationResponse"

	$response.Code = "ExplicitlyDenied";
	$response.MatchedRuleDescription = "nah mate";

	$logger.Trace($response.ResponseCode);


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

	$response = New-Object -TypeName "Lithnet.AccessManager.Web.Authorization.LapsHistoryAuthorizationResponse"

	$response.Code = "ExplicitlyDenied";
	$response.MatchedRuleDescription = "nah mate";

	$logger.Trace($response.ResponseCode);

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

	$response = New-Object -TypeName "Lithnet.AccessManager.Web.Authorization.JitAuthorizationResponse"

	$response.Code = "ExplicitlyDenied";
	$response.MatchedRuleDescription = "no way";
	$response.AuthorizingGroupName = $null;

	$logger.Trace($response.ResponseCode);

	Write-Output $response;
}