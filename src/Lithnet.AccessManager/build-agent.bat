@echo off
call action-do-pre-build.bat
if %errorlevel% neq 0 exit /b %errorlevel%

call action-new-version.bat
if %errorlevel% neq 0 exit /b %errorlevel%

call action-core-build-agent.bat
if %errorlevel% neq 0 exit /b %errorlevel%

ECHO %version%| clip
explorer %amsBuildFolder%
