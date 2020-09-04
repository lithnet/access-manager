@echo off
SETLOCAL
SET solutiondir=D:\dev\git\lithnet\access-manager\src\Lithnet.AccessManager
SET outputdir=%solutiondir%\Lithnet.AccessManager.Service.Setup\output
SET serviceProject=%solutiondir%\Lithnet.AccessManager.Service\Lithnet.AccessManager.Service.csproj
SET uiProject=%solutiondir%\Lithnet.AccessManager.Server.UI\Lithnet.AccessManager.Server.UI.csproj
SET setupProject=%solutiondir%\Lithnet.AccessManager.Service.Setup\Lithnet.AccessManager.Service.Setup.aip

rd /s /q "%outputdir%"
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet publish "%serviceProject%" --runtime win8-x64 --output "%outputdir%" --framework netcoreapp3.1 --self-contained false
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet publish "%uiproject%" --runtime win8-x64 --output "%outputdir%" --framework netcoreapp3.1 --self-contained false
if %errorlevel% neq 0 exit /b %errorlevel%

"%SIGNTOOLPATH%\signtool.exe" sign /sha1 %CSCERTTHUMBPRINT% /t http://timestamp.digicert.com /fd sha256 /v "%outputdir%\Lithnet*.exe"
if %errorlevel% neq 0 exit /b %errorlevel%

"%SIGNTOOLPATH%\signtool.exe" sign /sha1 %CSCERTTHUMBPRINT% /t http://timestamp.digicert.com /fd sha256 /v "%outputdir%\Lithnet*.dll"
if %errorlevel% neq 0 exit /b %errorlevel%

"%AIPATH%\AdvancedInstaller.com" /build "%setupProject%"
if %errorlevel% neq 0 exit /b %errorlevel%

ENDLOCAL

echo Service built with version %version%