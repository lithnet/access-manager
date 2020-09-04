@echo off
call pre-build.bat

call new-version.bat
if %errorlevel% neq 0 exit /b %errorlevel%

call build-service-core.bat
if %errorlevel% neq 0 exit /b %errorlevel%
