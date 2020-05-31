function Get-LapsAuthorizationResponse{
	param(
	[Lithnet.Laps.Web.ActiveDirectory.IUser]$user,
	[Lithnet.Laps.Web.ActiveDirectory.IComputer]$computer,
	[Nlog.ILogger]$logger
)

	$logger.Trace("We're in PowerShell!");
	$logger.Trace("Checking if $($user.SamAccountName) has access to $($computer.SamAccountName)");
	
	$response = New-Object -TypeName "Lithnet.Laps.Web.Authorization.AuthorizationResponse"
	
	$response.Code = "ExplicitlyDenied";
	$response.MatchedRuleDescription = "nah mate";
	
	$logger.Trace($response.ResponseCode);


	Write-Output $response;
}