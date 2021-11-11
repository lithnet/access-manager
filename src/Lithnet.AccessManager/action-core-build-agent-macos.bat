@echo off
SETLOCAL
SET agentProjectMacOs=%solutiondir%\Lithnet.AccessManager.Agent.MacOs\Lithnet.AccessManager.Agent.MacOs.csproj
SET outputdirx64MacOs=%amsBuildFolder%\macos-x64
SET outputdirarm64MacOs=%amsBuildFolder%\macos-arm64
SET pkgStagingPathx64=~/Documents/LithnetAccessManagerAgent-%version%.macos-x64.pkg
SET pkgStagingPatharm64=~/Documents/LithnetAccessManagerAgent-%version%.macos-arm64.pkg
SET macOSPackageFolder=/Volumes/ams
SET macOSBuildFolderx64=%macOSPackageFolder%/macos-x64
SET macOSBuildFolderarm64=%macOSPackageFolder%/macos-arm64
SET sshPath=C:\Program Files\Git\usr\bin

if NOT "%MacOSNotarizationAppleIDPasswordEncrypted%"=="" FOR /F "tokens=*" %%g IN ('powershell -File "%buildToolsPath%\dpapi-protection.ps1" -data "%MacOSNotarizationAppleIDPasswordEncrypted%"') do (SET MacOSNotarizationAppleIDPassword=%%g)  

If "%MacOSNotarizationAppleIDPassword%"=="" SET /P MacOSNotarizationAppleIDPassword=Please enter notarization password: 

REM ************************* macos installer *********************************

ECHO [92mClearing macos output directory[0m
powershell -Command "& Remove-Item '%outputdirx64MacOs%\*' -recurse -force" || exit /b %errorlevel%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mClearing old macos packages from the output directory[0m
powershell -Command "& Remove-Item '%amsBuildFolder%\*.pkg' -recurse -force" || exit /b %errorlevel%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO [92mBuilding 64-bit intel macOS package[0m
dotnet publish "%agentProjectMacOs%" --runtime osx-x64 --output "%outputdirx64MacOs%" --self-contained true /property:Version=%version% /property:FileVersion=%version% 

REM /p:TreatWarningsAsErrors=true /warnaserror
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
mkdir "%outputdirx64MacOs%\LithnetAccessManagerAgent"
move "%outputdirx64MacOs%\*" "%outputdirx64MacOs%\LithnetAccessManagerAgent"

ECHO [92mSigning x64 files[0m

"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "codesign --sign ""%MacOSDeveloperIDApplicationCertificate%"" --keychain %MacOSSigningCertificateKeychain% --verbose --options runtime --entitlements ""%macOSBuildFolderx64%/LithnetAccessManagerAgent/io.lithnet.accessmanager.agent.entitlements"" --timestamp %macOSBuildFolderx64%/LithnetAccessManagerAgent/*.dylib"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "codesign --sign ""%MacOSDeveloperIDApplicationCertificate%"" --keychain %MacOSSigningCertificateKeychain% --verbose --options runtime --entitlements ""%macOSBuildFolderx64%/LithnetAccessManagerAgent/io.lithnet.accessmanager.agent.entitlements"" --timestamp %macOSBuildFolderx64%/LithnetAccessManagerAgent/Lithnet.AccessManager.Agent"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "pkgbuild --identifier io.lithnet.accessmanager.agent --install-location /Applications --scripts ""%macOSBuildFolderx64%/scripts"" --keychain %MacOSSigningCertificateKeychain% --sign ""%MacOSDeveloperIDInstallerCertificate%"" --timestamp --root %macOSBuildFolderx64%/ %pkgStagingPathx64%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "xcrun notarytool submit %pkgStagingPathx64% --wait --apple-id %MacOSNotarizationAppleID% --team-id %MacOSNotarizationTeamID% --password %MacOSNotarizationAppleIDPassword%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "xcrun stapler staple %pkgStagingPathx64%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

"%sshPath%\scp.exe" -r "%MacOSBuildHostUsername%@%MacOSBuildHost%:%pkgStagingPathx64%" "%amsBuildFolder%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 

EXIT /b 0

ECHO [92mBuilding 64-bit arm macOS package[0m
dotnet publish "%agentProjectMacOs%" --runtime osx-arm64 --output "%outputdirarm64MacOs%" --framework net6.0 --self-contained true /property:Version=%version% /property:FileVersion=%version% /p:PublishSingleFile=true /p:DebugType=embedded

if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
mkdir "%outputdirarm64MacOs%\LithnetAccessManagerAgent"
move "%outputdirarm64MacOs%\*" "%outputdirarm64MacOs%\LithnetAccessManagerAgent"


ECHO [92mSigning arm64 files[0m

"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "codesign --sign ""%MacOSDeveloperIDApplicationCertificate%"" --keychain %MacOSSigningCertificateKeychain% --verbose --options runtime --entitlements ""%macOSBuildFolderarm64%/LithnetAccessManagerAgent/io.lithnet.accessmanager.agent.entitlements"" --timestamp %macOSBuildFolderarm64%/LithnetAccessManagerAgent/*.dylib"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "codesign --sign ""%MacOSDeveloperIDApplicationCertificate%"" --keychain %MacOSSigningCertificateKeychain% --verbose --options runtime --entitlements ""%macOSBuildFolderarm64%/LithnetAccessManagerAgent/io.lithnet.accessmanager.agent.entitlements"" --timestamp %macOSBuildFolderarm64%/LithnetAccessManagerAgent/Lithnet.AccessManager.Agent"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "pkgbuild --identifier io.lithnet.accessmanager.agent --install-location /Applications --scripts ""%macOSBuildFolderarm64%/scripts"" --keychain %MacOSSigningCertificateKeychain% --sign ""%MacOSDeveloperIDInstallerCertificate%"" --timestamp --root %macOSBuildFolderarm64%/ %pkgStagingPatharm64%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "xcrun notarytool submit %pkgStagingPatharm64% --wait --apple-id %MacOSNotarizationAppleID% --team-id %MacOSNotarizationTeamID% --password %MacOSNotarizationAppleIDPassword%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%
 
"%sshPath%\ssh.exe" %MacOSBuildHostUsername%@%MacOSBuildHost% "xcrun stapler staple %pkgStagingPatharm64%"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

"%sshPath%\scp.exe" -r "%MacOSBuildHostUsername%@%MacOSBuildHost%:%pkgStagingPatharm64%" "%amsBuildFolder%
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ENDLOCAL
