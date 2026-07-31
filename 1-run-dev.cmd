@echo off
setlocal EnableExtensions

set "ROOT=%~dp0"
set "API_PROJECT=%ROOT%src\Polar.Factograph.Api\Polar.Factograph.Api.csproj"
set "WEB_DIR=%ROOT%src\Polar.Factograph.Web"
set "WEBROOT=%ROOT%src\Polar.Factograph.Api\wwwroot"
set "PROJECT_CONFIG=%ROOT%factograph.project.json"
set "ONTOLOGY=%ROOT%ontology.xml"
set "RUNTIME_DIR=%ROOT%project-data\runtime\dev"
set "RUNTIME_SETTINGS=%ROOT%scripts\run\appsettings.json"

cd /d "%ROOT%" || goto :failed

call :require_file "%API_PROJECT%" "API project" || goto :failed
call :require_file "%PROJECT_CONFIG%" "factograph.project.json" || goto :failed
call :require_file "%ONTOLOGY%" "ontology.xml" || goto :failed
call :require_file "%RUNTIME_SETTINGS%" "runtime appsettings.json" || goto :failed

call :build_web || goto :failed

if not exist "%RUNTIME_DIR%" mkdir "%RUNTIME_DIR%"
if errorlevel 1 goto :failed
copy /Y "%RUNTIME_SETTINGS%" "%RUNTIME_DIR%\appsettings.json" >nul
if errorlevel 1 goto :failed

set "DOTNET_ENVIRONMENT=Development"
set "ASPNETCORE_ENVIRONMENT=Development"
set "Project__ConfigPath=%PROJECT_CONFIG%"
set "Authentication__Local__IdentityPath=%ROOT%project-data\identity.json"
set "Authentication__Local__DataProtectionKeysPath=%ROOT%project-data\data-protection-keys"
set "Authentication__Local__DefaultCassetteId=SypCassete"

echo.
echo Polar.Factograph development server
echo http://localhost:5000
echo Editors are read from scripts\run\appsettings.json
echo Press Ctrl+C to stop.
echo.

dotnet run -c Debug --project "%API_PROJECT%" --no-launch-profile -- ^
  --contentRoot "%RUNTIME_DIR%" ^
  --webroot "%WEBROOT%" ^
  --urls "http://localhost:5000"
set "EXIT_CODE=%ERRORLEVEL%"
if not "%EXIT_CODE%"=="0" goto :failed_with_code
exit /b 0

:build_web
pushd "%WEB_DIR%" || exit /b 1
echo Installing or updating React dependencies...
call npm install --package-lock=false --no-audit --no-fund
if errorlevel 1 (
  popd
  exit /b 1
)
echo Building React workspace...
call npm run build
set "BUILD_EXIT=%ERRORLEVEL%"
popd
exit /b %BUILD_EXIT%

:require_file
if exist "%~1" exit /b 0
echo ERROR: %~2 was not found:
echo   %~1
exit /b 1

:failed
set "EXIT_CODE=1"

:failed_with_code
echo.
echo Development launch failed with exit code %EXIT_CODE%.
pause
exit /b %EXIT_CODE%
