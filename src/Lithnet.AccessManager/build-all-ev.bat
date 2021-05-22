@echo off
SET /A buildingbeta=0

call action-do-pre-build.bat
if %errorlevel% neq 0 exit /b %errorlevel%

call action-new-version.bat
if %errorlevel% neq 0 exit /b %errorlevel%

call action-core-build-service.bat
if %errorlevel% neq 0 exit /b %errorlevel%

call action-core-build-agent.bat
if %errorlevel% neq 0 exit /b %errorlevel%

ECHO [92mSigning installers with EV certificate[0m
"%SIGNTOOLPATH%\signtool.exe" sign /sha1 %EVCSCERTTHUMBPRINT% /d "Lithnet Access Manager" /t http://timestamp.digicert.com /fd sha256 /v "%amsBuildFolder%\*"
if %errorlevel% neq 0 ECHO [91mBuild failed[0m && exit /b %errorlevel%

ECHO %version%| clip
explorer %amsBuildFolder%