@echo off
SETLOCAL
SET solutiondir=D:\dev\git\lithnet\access-manager\src\Lithnet.AccessManager
SET outputdir=%solutiondir%\Lithnet.AccessManager.Agent.Setup\output
SET agentProject=%solutiondir%\Lithnet.AccessManager.Agent\Lithnet.AccessManager.Agent.csproj

SET setupProjectx64=%solutiondir%\Lithnet.AccessManager.Agent.Setup\Lithnet.AccessManager.Agent.Setup-x64.aip
SET setupProjectx86=%solutiondir%\Lithnet.AccessManager.Agent.Setup\Lithnet.AccessManager.Agent.Setup-x86.aip

rd /s /q "%outputdir%"
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet publish "%agentProject%" --runtime win-x64 --output "%outputdir%" --framework net472 --self-contained false
if %errorlevel% neq 0 exit /b %errorlevel%

"C:\Program Files (x86)\Caphyon\Advanced Installer 17.3\bin\x86\AdvancedInstaller.com" /build "%setupProjectx64%"
if %errorlevel% neq 0 exit /b %errorlevel%


rd /s /q "%outputdir%"
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet publish "%agentProject%" --runtime win-x86 --output "%outputdir%" --framework net472 --self-contained false
if %errorlevel% neq 0 exit /b %errorlevel%

"C:\Program Files (x86)\Caphyon\Advanced Installer 17.3\bin\x86\AdvancedInstaller.com" /build "%setupProjectx86%"
if %errorlevel% neq 0 exit /b %errorlevel%


ENDLOCAL