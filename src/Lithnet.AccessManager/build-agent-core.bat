@echo off
SETLOCAL

SET solutiondir=D:\dev\git\lithnet\access-manager\src\Lithnet.AccessManager
SET outputdirx86=%solutiondir%\Lithnet.AccessManager.Agent.Setup\output-x86
SET outputdirx64=%solutiondir%\Lithnet.AccessManager.Agent.Setup\output-x64
SET agentProject=%solutiondir%\Lithnet.AccessManager.Agent\Lithnet.AccessManager.Agent.csproj

SET setupProjectx64=%solutiondir%\Lithnet.AccessManager.Agent.Setup\Lithnet.AccessManager.Agent.Setup-x64.aip
SET setupProjectx86=%solutiondir%\Lithnet.AccessManager.Agent.Setup\Lithnet.AccessManager.Agent.Setup-x86.aip

rd /s /q "%outputdirx64%"
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet publish "%agentProject%" --runtime win-x64 --output "%outputdirx64%" --framework net472 --self-contained false /property:Version=%version% /property:FileVersion=%version%
if %errorlevel% neq 0 exit /b %errorlevel%

"%SIGNTOOLPATH%\signtool.exe" sign /sha1 %CSCERTTHUMBPRINT% /d "Lithnet Access Manager" /t http://timestamp.digicert.com /fd sha256 /v "%outputdirx64%\Lithnet*.exe"
if %errorlevel% neq 0 exit /b %errorlevel%

"%SIGNTOOLPATH%\signtool.exe" sign /sha1 %CSCERTTHUMBPRINT% /d "Lithnet Access Manager" /t http://timestamp.digicert.com /fd sha256 /v "%outputdirx64%\Lithnet*.dll"
if %errorlevel% neq 0 exit /b %errorlevel%

"%AIPATH%\AdvancedInstaller.com" /build "%setupProjectx64%"
if %errorlevel% neq 0 exit /b %errorlevel%

rd /s /q "%outputdirx86%"
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet publish "%agentProject%" --runtime win-x86 --output "%outputdirx86%" --framework net472 --self-contained false  /property:Version=%version% /property:FileVersion=%version%
if %errorlevel% neq 0 exit /b %errorlevel%

"%SIGNTOOLPATH%\signtool.exe" sign /sha1 %CSCERTTHUMBPRINT% /d "Lithnet Access Manager" /t http://timestamp.digicert.com /fd sha256 /v "%outputdirx86%\Lithnet*.exe"
if %errorlevel% neq 0 exit /b %errorlevel%

"%SIGNTOOLPATH%\signtool.exe" sign /sha1 %CSCERTTHUMBPRINT% /d "Lithnet Access Manager" /t http://timestamp.digicert.com /fd sha256 /v "%outputdirx86%\Lithnet*.dll"
if %errorlevel% neq 0 exit /b %errorlevel%

"%AIPATH%\AdvancedInstaller.com" /build "%setupProjectx86%"
if %errorlevel% neq 0 exit /b %errorlevel%

ENDLOCAL

echo Agent built with version %version%