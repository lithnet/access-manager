<#

#>
param (
    $username, $SSHhost, $scriptPath, $packageLocation, $macOsSignerAppleId, $macOsSignerAppPassword, $appIdentifier, $macOsSignerTeamId
)

function SubmitNotarizationRequest($username, $SSHhost, $scriptPath, $packageLocation, $macOsSignerAppleId, $macOsSignerAppPassword, $appIdentifier, $macOsSignerTeamId)
{
    Write-Host "Attempting to submit notarization request"

    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = "C:\Program Files\Git\usr\bin\ssh.exe"
    $pinfo.RedirectStandardError = $true
    $pinfo.RedirectStandardOutput = $true
    $pinfo.UseShellExecute = $false
    $pinfo.Arguments = "$($username)@$($sshhost) `"$scriptPath --notarize -a `"$packageLocation`" -u $macosSignerAppleId -p $macosSignerAppPassword -b $appIdentifier -v $macosSignerTeamId`""
    
    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $pinfo
    $p.Start() | Out-Null
    $p.WaitForExit()

    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
            
    write-host "SSH command returned $($p.ExitCode)"
    write-host $stdout

    if ($p.ExitCode -ne 0)
    {
        throw "Ssh returned a non-zero exit code"
    }

    $x = ($stdout | Select-String -Pattern '<key>RequestUUID<\/key>\s+?<string>(.{36})<\/string>')

    if ($x.Matches.Count -ne 1)
    {
        throw "There was no request UUID detected in the response"
        exit 1
    }

    if ($x.Matches[0].Groups.Count -ne 2)
    {
        throw "There was no request UUID detected in the response"
        exit 2
    }

    $guid = $x.Matches[0].Groups[1].Value

    if ([Guid]::TryParse($guid,$([ref][guid]::Empty)))
    {
        Write-Output $guid
        return
    }

    throw "The request UUID could not be parsed"
    exit 3
}

function CheckNotarizationRequest($username, $SSHhost, $scriptPath, $macOsSignerAppleId, $macOsSignerAppPassword, $requestId)
{
    Write-Host "Attempting to check notarization request status"
    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = "C:\Program Files\Git\usr\bin\ssh.exe"
    $pinfo.RedirectStandardError = $true
    $pinfo.RedirectStandardOutput = $true
    $pinfo.UseShellExecute = $false
    $pinfo.Arguments = "$($username)@$($sshhost) `"$scriptPath -c -u $macosSignerAppleId -p $macosSignerAppPassword -k $requestId`""
  
    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $pinfo
    $p.Start() | Out-Null
    $p.WaitForExit()

    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
      
    
    write-host "SSH command returned $($p.ExitCode)"
    write-host $stdout
    write-host $stderr

    if ($p.ExitCode -ne 0)
    {
        #exit $p.ExitCode 
    }

    $x = ($stdout | Select-String -Pattern '<key>Status<\/key>\s+?<string>(success)<\/string>')

    if ($x.Matches.Count -eq 1)
    {
        Write-Output $true
        return
    }

    $x = ($stdout | Select-String -Pattern '<key>Status<\/key>\s+?<string>(in progress)<\/string>')

    if ($x.Matches.Count -eq 1)
    {
        Write-Output $false
        return
    }

    write-host "The notorization request was in an unknown state. Aborting"
    exit 4
}

function StapleNotarizationRequest($username, $SSHhost, $scriptPath, $packageLocation)
{
    Write-Host "Attempting to staple notarization result"
    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = "C:\Program Files\Git\usr\bin\ssh.exe"
    $pinfo.RedirectStandardError = $true
    $pinfo.RedirectStandardOutput = $true
    $pinfo.UseShellExecute = $false
    $pinfo.Arguments = "$($username)@$($sshhost) `"$scriptPath -s -a $packageLocation`""
    
    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $pinfo
    $p.Start() | Out-Null
    $p.WaitForExit()

    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
      
    
    write-host "SSH command returned $($p.ExitCode)"
    write-host $stdout   
}

$requestId = SubmitNotarizationRequest $username $SSHhost $scriptPath $packageLocation $macOsSignerAppleId $macOsSignerAppPassword $appIdentifier $macOsSignerTeamId

Write-host "Got request ID $requestId"
Write-host "Waiting 60 seconds before checking status" 

Sleep -Seconds 60 
$attempt = 0;

do
{
    $attempt++
    Write-host "Attempt #$attempt"
    $result = CheckNotarizationRequest $username $SSHhost $scriptPath $macOsSignerAppleId $macOsSignerAppPassword $requestId

    if ($result -ne $true)
    {
        write-host "Not yet approved, sleeping for 60 seconds"
        Sleep -Seconds 60
    }
    else
    {
        write-host "Approved!"
    }
}
while ($result -ne $true)

StapleNotarizationRequest $username $SSHhost $scriptPath $packageLocation