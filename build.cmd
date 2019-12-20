@echo off
setlocal

if NOT "x%YAMATO_JOB_ID%"=="x" set GITLAB_CI=1
if NOT "x%YAMATO_JOB_ID%"=="x" set CI_COMMIT_TAG=%GIT_TAG%
if NOT "x%YAMATO_JOB_ID%"=="x" set CI_COMMIT_REF_NAME=%GIT_BRANCH%

SET CONFIGURATION=Release
SET PUBLIC=""
SET BUILD=0
set UPM=0
set UNITYVERSION=2019.2

:loop
IF NOT "%1"=="" (
  IF "%1"=="-p" (
    SET PUBLIC=-p:PublicRelease=true
  )
  IF "%1"=="--public" (
    SET PUBLIC=-p:PublicRelease=true
  )
  IF "%1"=="-b" (
    SET BUILD=1
  )
  IF "%1"=="--build" (
    SET BUILD=1
  )
  IF "%1"=="-d" (
    SET CONFIGURATION=Debug
  )
  IF "%1"=="--debug" (
    SET CONFIGURATION=Debug
  )
  IF "%1"=="-r" (
    SET CONFIGURATION=Release
  )
  IF "%1"=="--release" (
    SET CONFIGURATION=Release
  )
  IF "%1"=="-u" (
    SET UPM=1
  )
  IF "%1"=="--upm" (
    SET UPM=1
  )
  IF "%1"=="-c" (
    SET CONFIGURATION=%2
    SHIFT
  )
  SHIFT
  GOTO :loop
)

if "%APPVEYOR%" == "" (
  common\nuget restore
  dotnet restore
)

dotnet build --no-restore -c %CONFIGURATION% %PUBLIC%

