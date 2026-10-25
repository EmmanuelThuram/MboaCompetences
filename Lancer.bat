@echo off
cd /d "%~dp0"
echo Compilation de MboaCompetences (Windows)...
dotnet build MboaCompetences\MboaCompetences.csproj -f net10.0-windows10.0.19041.0 --nologo -v q
if errorlevel 1 (echo Echec de la compilation. & pause & exit /b 1)
start "" "MboaCompetences\bin\Debug\net10.0-windows10.0.19041.0\win-x64\MboaCompetences.exe"
