@echo off
SET /A buildingbeta=0

call pre-build.bat
if %errorlevel% neq 0 exit /b %errorlevel%

call new-version.bat
if %errorlevel% neq 0 exit /b %errorlevel%

call build-service-core.bat
if %errorlevel% neq 0 exit /b %errorlevel%
