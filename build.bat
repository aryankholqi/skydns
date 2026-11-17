@echo off
REM Builds publish\SkyDNS.exe (single self-contained exe) and publish\SkyDNS-Setup.exe (installer)
dotnet publish SkyDNS\SkyDNS.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
if errorlevel 1 goto :fail

set "ISCC="
for %%P in ("%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" "%ProgramFiles%\Inno Setup 6\ISCC.exe") do if exist %%P set "ISCC=%%~P"
if not defined ISCC (
  echo Inno Setup 6 not found. Install it from https://jrsoftware.org/isdl.php
  goto :fail
)
"%ISCC%" /Q installer\SkyDNS.iss
if errorlevel 1 goto :fail

echo.
echo Done: publish\SkyDNS-Setup.exe
pause
exit /b 0

:fail
echo.
echo Build failed.
pause
exit /b 1
