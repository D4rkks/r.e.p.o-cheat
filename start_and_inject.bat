@echo off
set SMI=%~dp0SharpMonoInjector.Console\smi.exe
set DLL=%~dp0r.e.p.o cheat\bin\Release\r.e.p.o cheat.dll

echo Attente que REPO.exe soit lance...
:wait
tasklist /FI "IMAGENAME eq REPO.exe" 2>NUL | find /I "REPO.exe" >NUL
if errorlevel 1 (
    timeout /t 2 /nobreak >NUL
    goto wait
)

echo REPO.exe detecte. Injection en cours...
timeout /t 3 /nobreak >NUL

"%SMI%" inject -p REPO -a "%DLL%" -n r.e.p.o_cheat -c Loader -m Init

if errorlevel 1 (
    echo ERREUR: Injection echouee.
) else (
    echo Injection reussie!
)
pause
