@echo off
call action-do-pre-build.bat
if %errorlevel% neq 0 exit /b %errorlevel%

call action-new-version.bat
if %errorlevel% neq 0 exit /b %errorlevel%

call action-core-build-agent-macos.bat
if %errorlevel% neq 0 exit /b %errorlevel%

call action-core-build-agent-linux.bat
if %errorlevel% neq 0 exit /b %errorlevel%

call action-core-build-agent-windows.bat
if %errorlevel% neq 0 exit /b %errorlevel%

ECHO [92mAgent built with version %version%[0m
