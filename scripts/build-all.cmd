@echo off

set version=%1

set clean=%~dp0\clean.cmd
set buildpackage=%~dp0\build-package.cmd

if not exist "%clean%" (
  echo Could not find %clean%
  exit /b 1
)

if not exist "%buildpackage%" (
  echo Could not find %buildpackage%
  exit /b 1
)

call %buildpackage% Rebus %1
if %ERRORLEVEL% neq 0 (
  echo Could not build Rebus.
  exit /b 1
)

call %buildpackage% Rebus.Tests.Contracts %1
if %ERRORLEVEL% neq 0 (
  echo Could not build Rebus contracts.
  exit /b 1
)

call %buildpackage% Rebus.Forklift.Common %1
if %ERRORLEVEL% neq 0 (
  echo Could not build Rebus common forklift things.
  exit /b 1
)