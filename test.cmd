@echo off
setlocal

if "%~1"=="--yamato" goto yamato
goto normal

:yamato
call powershell scripts\Test.ps1 -Yamato
goto end

:normal
call powershell scripts\Test.ps1
goto end

:end
