@echo off
setlocal
rem Rebuilds the cheat under a unique assembly name and injects it into the running game,
rem so a new version can be loaded without restarting REPO (Mono can't unload assemblies).
set SMI=%~dp0SharpMonoInjector.Console\smi.exe
set OUT=%~dp0r.e.p.o cheat\bin\Release

rem Locate MSBuild through vswhere (ships with Visual Studio 2017+)
set VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
set MSBUILD=
for /f "usebackq delims=" %%M in (`"%VSWHERE%" -latest -prerelease -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do if not defined MSBUILD set "MSBUILD=%%M"
if not defined MSBUILD (
    echo ERREUR: MSBuild introuvable. Installe Visual Studio avec le workload .NET desktop.
    pause
    exit /b 1
)

echo Appuie sur F10 en jeu pour decharger l'ancienne version avant de continuer.
pause

del /q "%OUT%\r.e.p.o cheat_*.dll" "%OUT%\r.e.p.o cheat_*.pdb" 2>NUL

"%MSBUILD%" "%~dp0r.e.p.o cheat\r.e.p.o cheat.csproj" /p:Configuration=Release /p:HotReload=true /nologo /v:m
if errorlevel 1 (
    echo ERREUR: compilation echouee.
    pause
    exit /b 1
)

set DLL=
for /f "delims=" %%F in ('dir /b /o-d "%OUT%\r.e.p.o cheat_*.dll"') do if not defined DLL set "DLL=%OUT%\%%F"

echo Injection de %DLL%
"%SMI%" inject -p REPO -a "%DLL%" -n r.e.p.o_cheat -c Loader -m Init
pause
