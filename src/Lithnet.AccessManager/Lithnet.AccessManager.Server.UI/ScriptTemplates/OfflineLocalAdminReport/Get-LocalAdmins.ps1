<#
    .SYNOPSIS
    Get all local admin members excluding the builtin admin account.

    .DESCRIPTION
    While later versions of windows contain the Get-LocalUser cmdlet, this script uses the Windows API to obtain the local group membership, to ensure maximum compatibility with older versions of windows and PowerShell.
    
    This script needs to be run on each machine that you need to obtain the local administrators group membership from.  

    The script will create a CSV file at the path specified by the $reportPath variable. 
    
    If any errors are encountered, then a TXT file containing the error details will be produced instead.
    
    .PARAMETER
    [string]$ReportPath
    The location of the admin report share where the file should be saved
#>

param(
  [parameter(Mandatory=$true)]
  [string]$ReportPath
)

Begin{
  
  [string]$outFileName = "$reportPath\$($env:COMPUTERNAME).csv"         
  [string]$errorFileName = "$reportPath\$($env:COMPUTERNAME)-error.txt"
  $ErrorActionPreference='Stop'
  
  if (!(Test-Path -Path $reportPath)) 
  { 
    Write-Verbose -Message "$reportPath is not reachable or does not exist" 
    Exit 
  } 
}
  
Process{
  try
  {
    if (('WorkstationInfo' -as [type]) -eq $null)
    {
      Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
public static class WorkstationInfo
{
    [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int NetWkstaGetInfo(string serverName, int level, out IntPtr pWorkstationInfo);
    [DllImport("NetApi32.dll")]
    private static extern int NetApiBufferFree(IntPtr buffer);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WorkstationInfo100
    {
        public int PlatformID;
        public string ComputerName;
        public string LanGroup;
        public int MajorVersion;
        public int MinorVersion;
    }
    private static WorkstationInfo100 GetWorkstationInfo()
    {
        IntPtr pServerInfo = IntPtr.Zero;
        try
        {
            int result = NetWkstaGetInfo(null, 100, out pServerInfo);
            if (result != 0)
            {
                throw new Win32Exception(result);
            }
            var info = Marshal.PtrToStructure<WorkstationInfo100>(pServerInfo);
            return info;
        }
        finally
        {
            if (pServerInfo != IntPtr.Zero)
            {
                NetApiBufferFree(pServerInfo);
            }
        }
    }
    public static string GetMachineNTAccountName()
    {
        var result = GetWorkstationInfo();
        return result.LanGroup + "\\" + result.ComputerName;
    }
}
"@
    }
    $computerName = [WorkstationInfo]::GetMachineNTAccountName();
    $ADSI = [ADSI]"WinNT://localhost"
    $discard = $adsi.Children.SchemaFilter.Add("user")
    $localMachineSid = $null;
    $ADSI.Children | % {
      $sidBytes = $_.Properties["objectSid"][0]
      $name = $_.Properties["name"][0];
      $sid = New-Object "System.Security.Principal.SecurityIdentifier" $sidBytes, 0
      if ($sid.IsWellKnown([System.Security.Principal.WellKnownSidType]::AccountAdministratorSid))
      {
        $localMachineSid = $sid.AccountDomainSid
        return;
      }
    }
    $items = @();
    $discard = $adsi.Children.SchemaFilter.Clear()
    $discard = $adsi.Children.SchemaFilter.Add("group")
    $ADSI.Children | % {
      $sidBytes = $_.Properties["objectSid"][0]
      $name = $_.Properties["name"][0];
      $sid = New-Object "System.Security.Principal.SecurityIdentifier" $sidBytes, 0
      if ($sid.IsWellKnown([System.Security.Principal.WellKnownSidType]::BuiltinAdministratorsSid))
      {
        $_.Invoke("members") | % {
          $UserSidBytes = $_.GetType().InvokeMember("objectSid",  'GetProperty',  $null,  $_, $null)
          $userSid = New-Object "System.Security.Principal.SecurityIdentifier" $UserSidBytes, 0
          if ($userSid.AccountDomainSid -ne $localMachineSid)
          {
            $items += "$computerName,$usersid"
          }
        }
      }
    }
    $items | out-file -FilePath $outFileName
    $items
  }
  catch
  {
    Write-Error $_.ToString() 
    $_.ToString() | out-file -FilePath $errorFileName
  }
}
End{}