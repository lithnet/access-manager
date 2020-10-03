<#
.Synopsis
Create a new folder and file share for receiving local admin reports

.DESCRIPTION
This script create a new local folder and shares it. It configures the permissions so that only domain computers can create files in the folder, and they can only see and edit the files they create.
It will also create a subfolder called 'scripts' that can be used to distribute the script to the computers

.PARAMETER
[string]Path
The folder to create

.PARAMETER
[string]ShareName
The name of the share to create

.EXAMPLE
New-LocalAdminReportShare -Path 'c:\local-admin-reports' -ShareName 'local-admin-reports'
      
#>

param( 
[Parameter(Mandatory = $true)] 
[string]$Path, 

[Parameter(Mandatory = $true)] 
[string]$ShareName
) 

if (-not (Test-Path $path))
{
    New-Item $path -ItemType 'directory' | out-null
}

$acl = new-object System.Security.AccessControl.DirectorySecurity
$acl.SetAccessRuleProtection($true,$false)

$accessRule1 = New-Object System.Security.AccessControl.FileSystemAccessRule("NT Authority\System","FullControl","ContainerInherit,ObjectInherit","None","Allow")
$accessRule2 = New-Object System.Security.AccessControl.FileSystemAccessRule("NT Authority\Interactive","Read","ContainerInherit,ObjectInherit","None","Allow")
$accessRule3 = New-Object System.Security.AccessControl.FileSystemAccessRule("Builtin\Administrators","FullControl","ContainerInherit,ObjectInherit","None","Allow")
$accessRule3 = New-Object System.Security.AccessControl.FileSystemAccessRule("Domain Admins","FullControl","ContainerInherit,ObjectInherit","None","Allow")
$accessRule4 = New-Object System.Security.AccessControl.FileSystemAccessRule("Domain Computers","CreateFiles","None","None","Allow")
$accessRule5 = New-Object System.Security.AccessControl.FileSystemAccessRule("NT Authority\CREATOR OWNER","Write, ReadAndExecute, Synchronize",'ObjectInherit',"InheritOnly","Allow")

$acl.AddAccessRule($accessRule1)
$acl.AddAccessRule($accessRule2)
$acl.AddAccessRule($accessRule3)
$acl.AddAccessRule($accessRule4)
$acl.AddAccessRule($accessRule5)

$acl | Set-Acl $path

$scriptPath = "$path\scripts"
if (-not (Test-Path $scriptPath))
{
    New-Item $scriptPath -ItemType 'directory' | out-null
}

$acl = Get-Acl $scriptPath

$accessRule1 = New-Object System.Security.AccessControl.FileSystemAccessRule("Domain Computers","Read","ContainerInherit,ObjectInherit","None","Allow")
$accessRule2 = New-Object System.Security.AccessControl.FileSystemAccessRule("Domain Users","Read","ContainerInherit,ObjectInherit","None","Allow")

$acl.AddAccessRule($accessRule1)
$acl.AddAccessRule($accessRule2)

$acl | Set-Acl $scriptPath

if ((Get-SmbShare -Name $shareName -ErrorAction SilentlyContinue) -eq $null)
{
    New-SmbShare -ChangeAccess "Domain Computers" -FullAccess @("Domain Admins","Builtin\Administrators") -Path $path -name $shareName | out-null
    Write-Host "Created new share"
}
else
{
    Write-Warning "A share with the same name already exists"
}
