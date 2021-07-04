@echo off
SETLOCAL

SET solutiondir=D:\dev\git\lithnet\access-manager\src\Lithnet.AccessManager
SET outputdirx86=%solutiondir%\Lithnet.AccessManager.Agent.Setup\output-x86
SET outputdirx64=%solutiondir%\Lithnet.AccessManager.Agent.Setup\output-x64
SET agentProject=%solutiondir%\Lithnet.AccessManager.Agent\Lithnet.AccessManager.Agent.csproj

SET setupProjectx64=%solutiondir%\Lithnet.AccessManager.Agent.Setup\Lithnet.AccessManager.Agent.Setup-x64.aip
SET setupProjectx86=%solutiondir%\Lithnet.AccessManager.Agent.Setup\Lithnet.AccessManager.Agent.Setup-x86.aip
SET intuneBuildDirx64=%amsBuildFolder%\intune\x64
SET intuneBuildDirx86=%amsBuildFolder%\intune\x86

REM ************************* 64-bit installer *********************************

ECHO [92mClearing 64-bit intune output directory[0m
IF EXIST "%intuneBuildDirx64%" powershell -Command "& Remove-Item '%intuneBuildDirx64%\*' -recurse -force" || exit /b %errorlevel%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
IF NOT EXIST "%intuneBuildDirx64%" md "%intuneBuildDirx64%" || exit /b %errorlevel%

ECHO [92mClearing 64-bit output directory[0m
IF EXIST "%outputdirx64%" powershell -Command "& Remove-Item '%outputdirx64%\*' -recurse -force" || exit /b %errorlevel%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mBuilding 64-bit agent project[0m
dotnet publish "%agentProject%" --runtime win-x64 --output "%outputdirx64%" --framework net472 --self-contained false /property:Version=%version% /property:FileVersion=%version%  /p:TreatWarningsAsErrors=true /warnaserror 
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mSigning 64-bit Lithnet EXEs[0m
"%SIGNTOOLPATH%\signtool.exe" sign /sha1 %CSCERTTHUMBPRINT% /d "Lithnet Access Manager" /t http://timestamp.digicert.com /fd sha256 /v "%outputdirx64%\Lithnet*.exe"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mSigning 64-bit Lithnet DLLs[0m
"%SIGNTOOLPATH%\signtool.exe" sign /sha1 %CSCERTTHUMBPRINT% /d "Lithnet Access Manager" /t http://timestamp.digicert.com /fd sha256 /v "%outputdirx64%\Lithnet*.dll"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mBuilding 64-bit installer[0m
"%AIPATH%\AdvancedInstaller.com" /build "%setupProjectx64%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mCreating 64-bit intune package[0m
echo  "%amsBuildFolder%\Lithnet Access Manager Agent Setup-x64.msi" "%intuneBuildDirx64%" 
copy "%amsBuildFolder%\Lithnet Access Manager Agent Setup-x64.msi" "%intuneBuildDirx64%" /y
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
%devToolsPath%\IntuneWinAppUtil.exe -c "%intuneBuildDirx64%" -s "Lithnet Access Manager Agent Setup-x64.msi" -o "%amsBuildFolder%" -q
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

REM ************************* 32-bit installer *********************************

ECHO [92mClearing 32-bit intune output directory[0m
IF EXIST "%intuneBuildDirx86%" powershell -Command "& Remove-Item '%intuneBuildDirx86%\*' -recurse -force" || exit /b %errorlevel%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
IF NOT EXIST "%intuneBuildDirx86%" md "%intuneBuildDirx86%" || exit /b %errorlevel%

ECHO [92mClearing 32-bit output directory[0m
IF EXIST "%outputdirx86%" powershell -Command "& Remove-Item '%outputdirx86%\*' -recurse -force" || exit /b %errorlevel%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mBuilding 32-bit agent project[0m
dotnet publish "%agentProject%" --runtime win-x86 --output "%outputdirx86%" --framework net472 --self-contained false  /property:Version=%version% /property:FileVersion=%version%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mSigning 32-bit Lithnet EXEs[0m
"%SIGNTOOLPATH%\signtool.exe" sign /sha1 %CSCERTTHUMBPRINT% /d "Lithnet Access Manager" /t http://timestamp.digicert.com /fd sha256 /v "%outputdirx86%\Lithnet*.exe"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mSigning 32-bit Lithnet DLLs[0m
"%SIGNTOOLPATH%\signtool.exe" sign /sha1 %CSCERTTHUMBPRINT% /d "Lithnet Access Manager" /t http://timestamp.digicert.com /fd sha256 /v "%outputdirx86%\Lithnet*.dll"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mBuilding 32-bit installer[0m
"%AIPATH%\AdvancedInstaller.com" /build "%setupProjectx86%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%


ECHO [92mCreating 32-bit intune package[0m
echo  "%amsBuildFolder%\Lithnet Access Manager Agent Setup-x86.msi" "%intuneBuildDirx86%" 
copy "%amsBuildFolder%\Lithnet Access Manager Agent Setup-x86.msi" "%intuneBuildDirx86%" /y
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
%devToolsPath%\IntuneWinAppUtil.exe -c "%intuneBuildDirx86%" -s "Lithnet Access Manager Agent Setup-x86.msi" -o "%amsBuildFolder%" -q
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%


ENDLOCAL

ECHO [92mAgent built with version %version%[0m
