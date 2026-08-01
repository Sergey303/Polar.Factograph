@echo off
setlocal EnableExtensions

set "ROOT=%~dp0"
set "API_PROJECT=%ROOT%src\Polar.Factograph.Api\Polar.Factograph.Api.csproj"
set "PROJECT_CONFIG=%ROOT%factograph.project.json"
set "ONTOLOGY=%ROOT%ontology.xml"
set "RUNTIME_SETTINGS=%ROOT%scripts\run\appsettings.json"

if "%~1"=="" (
  set "TARGET=%ROOT%publish\Polar.Factograph"
) else (
  set "TARGET=%~f1"
)

cd /d "%ROOT%" || goto :failed

call :require_file "%API_PROJECT%" "API project" || goto :failed
call :require_file "%PROJECT_CONFIG%" "factograph.project.json" || goto :failed
call :require_file "%ONTOLOGY%" "ontology.xml" || goto :failed
call :require_file "%RUNTIME_SETTINGS%" "runtime appsettings.json" || goto :failed

echo Publishing Polar.Factograph to:
echo   %TARGET%
echo.

dotnet publish "%API_PROJECT%" -c Release -o "%TARGET%"
if errorlevel 1 goto :failed

copy /Y "%RUNTIME_SETTINGS%" "%TARGET%\appsettings.json" >nul
if errorlevel 1 goto :failed

call :require_file "%TARGET%\Polar.Factograph.Api.dll" "published API" || goto :failed
call :require_file "%TARGET%\wwwroot\index.html" "published React application" || goto :failed

dotnet dev-certs https --check >nul 2>&1
if errorlevel 1 (
  echo ERROR: an HTTPS development certificate is required for the local Production launch.
  echo Run this command once and then start the shortcut again:
  echo   dotnet dev-certs https --trust
  goto :failed
)

set "DOTNET_ENVIRONMENT=Production"
set "ASPNETCORE_ENVIRONMENT=Production"
set "Project__ConfigPath=%PROJECT_CONFIG%"
set "Authentication__Local__IdentityPath=%ROOT%project-data\identity.json"
set "Authentication__Local__DataProtectionKeysPath=%ROOT%project-data\data-protection-keys"

echo.
echo Published application:
echo   %TARGET%
echo HTTPS address:
echo   https://localhost:5001
echo Editors are read from scripts\run\appsettings.json
echo Press Ctrl+C to stop.
echo.

dotnet "%TARGET%\Polar.Factograph.Api.dll" ^
  --contentRoot "%TARGET%" ^
  --webroot "%TARGET%\wwwroot" ^
  --urls "https://localhost:5001"
set "EXIT_CODE=%ERRORLEVEL%"
if not "%EXIT_CODE%"=="0" goto :failed_with_code
exit /b 0

:require_file
if exist "%~1" exit /b 0
echo ERROR: %~2 was not found:
echo   %~1
exit /b 1

:failed
set "EXIT_CODE=1"

:failed_with_code
echo.
echo Release publish or launch failed with exit code %EXIT_CODE%.
pause
exit /b %EXIT_CODE%
