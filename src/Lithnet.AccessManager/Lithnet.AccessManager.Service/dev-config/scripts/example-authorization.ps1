function Get-AuthorizationResponse{
	param(
	$user,
	$computer
)

	# See https://go.lithnet.io/fwlink/oyotjigz for help

	Write-Information  "We're in PowerShell!"
	Write-Information "Checking if $($user.MsDsPrincipalName) is allowed access to $($computer.MsDsPrincipalName)"

	# Create an object to hold our authorization decisions
	# Set IsAllowed to true to allow access, or set IsDenied to explicitly deny access, or leave both as false if no decision was made. This will allow other rules to be evaluated.
	$response = [pscustomobject]@{
		IsLocalAdminPasswordAllowed = $false
		IsLocalAdminPasswordDenied = $false
		IsLocalAdminPasswordHistoryAllowed = $false
		IsLocalAdminPasswordHistoryDenied = $false
		IsJitAllowed = $false
		IsJitDenied = $false
	}

	# Return the authorization response to Access Manager to process
	Write-Output $response;
}