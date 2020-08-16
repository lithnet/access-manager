@echo off
SETLOCAL
SET solutiondir=D:\dev\git\lithnet\access-manager\src\Lithnet.AccessManager
SET outputdir=%solutiondir%\Lithnet.AccessManager.Service.Setup\output
SET serviceProject=%solutiondir%\Lithnet.AccessManager.Web\Lithnet.AccessManager.Web.csproj
SET uiProject=%solutiondir%\Lithnet.AccessManager.Server.UI\Lithnet.AccessManager.Server.UI.csproj
SET setupProject=%solutiondir%\Lithnet.AccessManager.Service.Setup\Lithnet.AccessManager.Service.Setup.aip

rd /s /q "%outputdir%"
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet publish "%serviceProject%" --runtime win-x64 --output "%outputdir%" --framework netcoreapp3.1 --self-contained false
if %errorlevel% neq 0 exit /b %errorlevel%

"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\msbuild" "%uiproject%" /p:OutputPath="%outputdir%"
if %errorlevel% neq 0 exit /b %errorlevel%

"C:\Program Files (x86)\Caphyon\Advanced Installer 17.3\bin\x86\AdvancedInstaller.com" /build "%setupProject%"
if %errorlevel% neq 0 exit /b %errorlevel%


ENDLOCAL