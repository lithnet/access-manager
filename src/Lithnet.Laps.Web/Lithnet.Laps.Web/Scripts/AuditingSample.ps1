function Write-AuditLog{
	param(
	[hashtable]$tokens,
	[bool]$isSuccess,
	[Nlog.ILogger]$logger
)

	$logger.Trace("We're in PowerShell for auditing!");
	
	$tokens.Keys | % {
		Write-Host "$($_):$($tokens.Item($_))";
		$logger.Trace( "$($_):$($tokens.Item($_))");
		};
}