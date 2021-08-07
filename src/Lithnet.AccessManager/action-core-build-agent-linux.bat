@echo off
SETLOCAL

SET outputdirx64Linux=D:\dev\installers\ams
SET agentProjectLinux=%solutiondir%\Lithnet.AccessManager.Agent.Linux\Lithnet.AccessManager.Agent.Linux.csproj

REM ************************* linux installer *********************************

ECHO [92mClearing old linux packages from the output directory[0m
powershell -Command "& Remove-Item '%outputdirx64Linux%\*.rpm' -recurse -force" || exit /b %errorlevel%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

powershell -Command "& Remove-Item '%outputdirx64Linux%\*.deb' -recurse -force" || exit /b %errorlevel%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

powershell -Command "& Remove-Item '%outputdirx64Linux%\*.tar.gz' -recurse -force" || exit /b %errorlevel%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%


ECHO [92mBuilding Intel 64-bit DEB package[0m
dotnet publish "%agentProjectLinux%" --runtime linux-x64 --framework netcoreapp3.1 --self-contained true /property:Version=%version% /property:FileVersion=%version%  /p:TreatWarningsAsErrors=true /warnaserror /target:CreateDeb /p:PackageDir="%amsBuildFolder%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mBuilding Intel 64-bit RPM package[0m
dotnet publish "%agentProjectLinux%" --runtime linux-x64 --framework netcoreapp3.1 --self-contained true /property:Version=%version% /property:FileVersion=%version%  /p:TreatWarningsAsErrors=true /warnaserror /target:CreateRpm /p:PackageDir="%amsBuildFolder%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mBuilding Intel 64-bit Tarball package[0m
dotnet publish "%agentProjectLinux%" --runtime linux-x64 --framework netcoreapp3.1 --self-contained true /property:Version=%version% /property:FileVersion=%version%  /p:TreatWarningsAsErrors=true /warnaserror /target:CreateTarBall /p:PackageDir="%amsBuildFolder%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%


ECHO [92mBuilding ARM 64-bit DEB package[0m
dotnet publish "%agentProjectLinux%" --runtime linux-arm64 --framework netcoreapp3.1 --self-contained true /property:Version=%version% /property:FileVersion=%version%  /p:TreatWarningsAsErrors=true /warnaserror /target:CreateDeb /p:PackageDir="%amsBuildFolder%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mBuilding ARM 64-bit RPM package[0m
dotnet publish "%agentProjectLinux%" --runtime linux-arm64 --framework netcoreapp3.1 --self-contained true /property:Version=%version% /property:FileVersion=%version%  /p:TreatWarningsAsErrors=true /warnaserror /target:CreateRpm /p:PackageDir="%amsBuildFolder%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mBuilding ARM 64-bit Tarball package[0m
dotnet publish "%agentProjectLinux%" --runtime linux-arm64 --framework netcoreapp3.1 --self-contained true /property:Version=%version% /property:FileVersion=%version%  /p:TreatWarningsAsErrors=true /warnaserror /target:CreateTarBall /p:PackageDir="%amsBuildFolder%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%


ENDLOCAL
