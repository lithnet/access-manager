<#
    .Synopsis
    Validates and merges individual admin report CSVs into a single file

    .DESCRIPTION
    This script takes a path to a directory containing multiple csv files created by the Get-LocalAdmin.ps1 script and merges them into a single csv file. 
    For security reasons, the script also ensures that the file was created by the computer it purports to come from.

    .PARAMETER
    [string]CSVPath
    Path to the directory containing the local admin reports

    .PARAMETER
    [string]OutFile
    The name of the consolidated file to create

    .PARAMETER
    [switch]IgnoreOwnerErrors
    By default, any local admin reports where the owner of the file is not the computer the file purports to be for will be excluded from the consolidated report. Include this switch to ignore these warnings and include the files in the merged report

    .EXAMPLE
    Get-ValidatedAndMergedLocalAdminFile -CSVPath 'c:\local-admin-reports' -OutFile 'c:\mergedresults\merge.csv'
      
#>
param( 
  [Parameter(Mandatory = $true)] 
  [string]$CSVPath, 

  [Parameter(Mandatory = $true)] 
  [string]$OutFile, 

  [Parameter(Mandatory = $false)] 
  [Switch]$IgnoreOwnerErrors 
) 
  
Begin
{
  $ErrorActionPreference='Stop'
  $WarningActionPreference='Continue'

  if (!(Test-Path -Path $OutFile)) 
  { 
    Write-Verbose -Message "Creating $OutFile" 
    New-Item -Path $OutFile -Force -ItemType File | out-null
  } 

  $Merged = @(); 
}
Process
{
  [int]$i = 0
  $CSVs = Get-ChildItem -Path $CSVPath

  foreach($CSV in $CSVs.FullName) 
  { 
    Write-Progress -Activity 'Merging files...' -Status $CSV -PercentComplete ((($i++) / $CSVs.count) * 100)
    
    if(-not (Test-Path $CSV))
    { 
      Write-Warning "Does not exist: $CSV" 
      continue;
    }

    $acl = Get-Acl $csv
    try
    {
      $ownerFqName = $acl.GetOwner([System.Security.Principal.NTAccount]).Value.TrimEnd('$');
    
      if ($ownerFqName.Contains("\"))
      {
        $owner = $ownerFqName.Split('\')[1]
      }
      else
      {
        $owner = $ownerFqName
      }

      $fileName = [System.IO.Path]::GetFileNameWithoutExtension($csv)

      if ($owner -ne $fileName)
      {
        if (-not $IgnoreOwnerErrors)
        {
          Write-Warning "File $csv was not owned by the expected principal. The owner of the file was $owner, but was expected to be $filename. This file has been skipped. Use the -IgnoreOwnerErrors switch to include these files"
          continue;
        }
        else
        {
          Write-Warning "File $csv was not owned by the expected principal. The owner of the file was $owner, but was expected to be $filename"
        }
      }
      
      $CSV = Import-CSV -Path $CSV -Header Host,SID

      foreach ($row in $csv)
      {
        if ($row.Host -ne $ownerFqName)
        {
          if (-not $IgnoreOwnerErrors)
          {
            Write-Warning "File contained an unexpected entry. The file should only contain records for the computer $ownerFqName, but an entry was found for $($row.Host). This record has been skipped. Use the -IgnoreOwnerErrors switch to include these records"
            continue;
          }
          else
          {
            Write-Warning "File contained an unexpected entry. The file should only contain records for the computer $ownerFqName, but an entry was found for $($row.Host)"
          }
        }
      }
      
      $Merged += $CSV 
    }   
    catch
    {
      Write-Warning "Unable to merge file $csv. $($_.Exception.Message)"
      continue;
    } 
  }
}

End
{
  $Merged | Export-Csv -Path $OutFile -NoTypeInformation 
      Write-Output "$OutFile successfully created" 
}
