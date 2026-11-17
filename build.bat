@echo off
REM Builds a single self-contained SkyDNS.exe into .\publish
dotnet publish SkyDNS\SkyDNS.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
echo.
echo Done: publish\SkyDNS.exe
pause
