@echo off
SETLOCAL
SET agentProjectMacOs=%solutiondir%\Lithnet.AccessManager.Agent.MacOs\Lithnet.AccessManager.Agent.MacOs.csproj
SET outputdirx64MacOs=%amsBuildFolder%\macos-x64
SET outputdirarm64MacOs=%amsBuildFolder%\macos-arm64
SET pkgStagingPath=~/Documents/LithnetAccessManagerAgent-%version%.macos-x64.pkg
SET macOSPackageFolder=/Volumes/ams
SET macOSBuildFolder=%macOSPackageFolder%/macos-x64
SET sshPath=C:\Program Files\Git\usr\bin

If "%MacOSNotarizationAppleIDPassword%"=="" SET /P MacOSNotarizationAppleIDPassword=Please enter notarization password: 

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

ECHO [92mSigning files[0m

"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "codesign --sign ""%MacOSDeveloperIDApplicationCertificate%"" --keychain %MacOSSigningCertificateKeychain% --verbose --options runtime --entitlements ""%macOSBuildFolder%/LithnetAccessManagerAgent/io.lithnet.accessmanager.agent.entitlements"" --timestamp %macOSBuildFolder%/LithnetAccessManagerAgent/*.dylib"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "codesign --sign ""%MacOSDeveloperIDApplicationCertificate%"" --keychain %MacOSSigningCertificateKeychain% --verbose --options runtime --entitlements ""%macOSBuildFolder%/LithnetAccessManagerAgent/io.lithnet.accessmanager.agent.entitlements"" --timestamp %macOSBuildFolder%/LithnetAccessManagerAgent/Lithnet.AccessManager.Agent"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "pkgbuild --identifier io.lithnet.accessmanager.agent --install-location /Applications --scripts ""%macOSBuildFolder%/scripts"" --keychain %MacOSSigningCertificateKeychain% --sign ""%MacOSDeveloperIDInstallerCertificate%"" --timestamp --root %macOSBuildFolder%/ %pkgStagingPath%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "xcrun notarytool submit %pkgStagingPath% --wait --apple-id %MacOSNotarizationAppleID% --team-id %MacOSNotarizationTeamID% --password %MacOSNotarizationAppleIDPassword%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "xcrun stapler staple %pkgStagingPath%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

"%sshPath%\scp.exe" -r "%MacOSBuildHostUsername%@%MacOSBuildHost%:%pkgStagingPath%" "%amsBuildFolder%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
ENDLOCAL
