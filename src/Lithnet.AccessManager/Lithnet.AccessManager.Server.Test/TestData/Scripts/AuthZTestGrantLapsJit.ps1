function Get-AuthorizationResponse{
	param(
	$user,
	$computer
)

	# See https://github.com/lithnet/access-manager/wiki/Authorization-Scripts for help

	Write-Information  "We're in PowerShell!"
	Write-Information "Checking if $($user.MsDsPrincipalName) is allowed access to $($computer.MsDsPrincipalName)"

	# Create an object to hold our authorization decisions
	# Set IsAllowed to true to allow access, or set IsDenied to explicitly deny access, or leave both as false if no decision was made. This will allow other rules to be evaluated.
	$response = [pscustomobject]@{
		IsLocalAdminPasswordAllowed = $true
		IsLocalAdminPasswordDenied = $false
		IsLocalAdminPasswordHistoryAllowed = $false
		IsLocalAdminPasswordHistoryDenied = $false
		IsJitAllowed = $true
		IsJitDenied = $false
	}

	# Return the authorization response to Access Manager to process
	Write-Output $response;
}