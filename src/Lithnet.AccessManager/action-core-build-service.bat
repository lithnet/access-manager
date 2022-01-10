@echo off
SETLOCAL
SET solutiondir=D:\dev\git\lithnet\access-manager\src\Lithnet.AccessManager
SET outputdirservice=%solutiondir%\Lithnet.AccessManager.Service.Setup\output
SET outputdirps=%solutiondir%\Lithnet.AccessManager.Service.Setup\output-ps
SET serviceProject=%solutiondir%\Lithnet.AccessManager.Service\Lithnet.AccessManager.Service.csproj
SET uiProject=%solutiondir%\Lithnet.AccessManager.Server.UI\Lithnet.AccessManager.Server.UI.csproj
SET powerShellProject=%solutiondir%\Lithnet.AccessManager.PowerShell\Lithnet.AccessManager.PowerShell.csproj
SET setupProject=%solutiondir%\Lithnet.AccessManager.Service.Setup\Lithnet.AccessManager.Service.Setup.aip

ECHO [92mClearing output directory "%outputdirservice%"[0m
IF EXIST "%outputdirservice%" powershell -Command "& Remove-Item '%outputdirservice%\*' -recurse -force" || exit /b %errorlevel%
if %errorlevel% neq 0 exit /b %errorlevel%

ECHO [92mClearing output directory "%outputdirps%"[0m
IF EXIST "%outputdirps%" powershell -Command "& Remove-Item '%outputdirps%\*' -recurse -force" || exit /b %errorlevel%
if %errorlevel% neq 0 exit /b %errorlevel%

ECHO [92mCreating output directory "%outputdirservice%"[0m
IF NOT EXIST "%outputdirservice%" md %outputdirservice% || exit /b %errorlevel%

ECHO [92mCreating output directory "%outputdirps%"[0m
IF NOT EXIST "%outputdirps%" md %outputdirps% || exit /b %errorlevel%



if "%buildingbeta%" EQU "1" (
ECHO [94mWriting license file[0m
"D:\dev\git\lithnet\Lithnet.Licensing\src\Lithnet.Licensing.Cli\bin\Debug\netcoreapp3.1\Lithnet.Licensing.Cli.exe" --licensed-to "Beta program participant" --product-name "Lithnet Access Manager" --product-id  "B5ABACF5-BB38-40CC-BEC9-7D6013AA3FF0" --type "BuiltIn" --feature-mask 0xFFFFFFFF --sku 1 --audiences "*" --units "1" --unit-type "Instance" --min-version "%version%" --max-version "%version%" --expire-in-days "90" --out-file "%outputdirservice%\license.dat"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
)

ECHO [92mBuilding service project[0m
dotnet publish "%serviceProject%" --runtime win8-x64 --output "%outputdirservice%" --framework netcoreapp3.1 --self-contained false  
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mBuilding UI project[0m
dotnet publish "%uiproject%" --runtime win8-x64 --output "%outputdirservice%" --framework netcoreapp3.1 --self-contained false  
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mBuilding PowerShell project[0m
dotnet publish "%powerShellProject%" --output "%outputdirps%" --runtime win-x64 --framework net472 --self-contained false --configuration Release /property:Version=%version% /property:FileVersion=%version% 
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mSigning Lithnet EXEs[0m
"%SIGNTOOLPATH%\signtool.exe" sign /sha1 %CSCERTTHUMBPRINT% /d "Lithnet Access Manager" /t http://timestamp.digicert.com /fd sha256 /v "%outputdirservice%\Lithnet*.exe"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mSigning Lithnet DLLs[0m
"%SIGNTOOLPATH%\signtool.exe" sign /sha1 %CSCERTTHUMBPRINT% /d "Lithnet Access Manager" /t http://timestamp.digicert.com /fd sha256 /v "%outputdirservice%\Lithnet*.dll"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mBuilding installer[0m
"%AIPATH%\AdvancedInstaller.com" /build "%setupProject%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ENDLOCAL

ECHO [92mService built with version %version%[0m
