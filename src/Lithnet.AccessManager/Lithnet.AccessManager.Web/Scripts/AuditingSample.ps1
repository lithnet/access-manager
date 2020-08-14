function Write-AuditLog{
	param(
	[hashtable]$tokens,
	[bool]$isSuccess,
)

	Write-Information "We're in PowerShell for auditing!";

	$tokens.Keys | % {
		Write-Information "$($_):$($tokens.Item($_))";
		};
}