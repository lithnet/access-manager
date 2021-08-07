@echo off
SETLOCAL

SET agentProjectMacOs=%solutiondir%\Lithnet.AccessManager.Agent.MacOs\Lithnet.AccessManager.Agent.MacOs.csproj
SET outputdirx64MacOs=%amsBuildFolder%\macos-x64
SET outputdirarm64MacOs=%amsBuildFolder%\macos-arm64

REM ************************* macos installer *********************************

ECHO [92mClearing macos output directory[0m
powershell -Command "& Remove-Item '%outputdirx64MacOs%\*' -recurse -force" || exit /b %errorlevel%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mClearing old macos packages from the output directory[0m
powershell -Command "& Remove-Item '%amsBuildFolder%\*.pkg' -recurse -force" || exit /b %errorlevel%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mBuilding 64-bit intel macOS package[0m
dotnet publish "%agentProjectMacOs%" --runtime osx-x64 --output "%outputdirx64MacOs%" --framework netcoreapp3.1 --self-contained true /property:Version=%version% /property:FileVersion=%version% 
REM /p:TreatWarningsAsErrors=true /warnaserror
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
mkdir "%outputdirx64MacOs%\LithnetAccessManagerAgent"
move "%outputdirx64MacOs%\*" "%outputdirx64MacOs%\LithnetAccessManagerAgent"

"C:\Program Files\Git\usr\bin\ssh.exe" ryannewington@intelmacmini.lithnet.local "pkgbuild --identifier io.lithnet.accessmanager.agent --install-location /Applications --scripts ""/Volumes/ams/macos-x64/scripts"" --keychain /Library/Keychains/System.keychain --sign ""Developer ID Installer: Lithnet Pty Ltd (5DK86QQXK3)"" --timestamp --root /Volumes/ams/macos-x64/ ~/Documents/LithnetAccessManagerAgent-%version%.macos-x64.pkg"

"C:\Program Files\Git\usr\bin\scp.exe" -r "ryannewington@intelmacmini.lithnet.local:~/Documents/LithnetAccessManagerAgent-%version%.macos-x64.pkg" "%amsBuildFolder%

ENDLOCAL
