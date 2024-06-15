@echo off

:: Manually set the Unity executable path
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2019.2.1f1\Editor\Unity.exe"
echo UNITY_PATH: %UNITY_PATH%
set PROJECT_PATH=%cd%
echo PROJECT_PATH: %PROJECT_PATH%
set METHOD_NAME="QuickPrimitives.Editor.CLI.Run"
echo 

setlocal enabledelayedexpansion

:: Check if the Unity executable exists
if not exist %UNITY_PATH% (
    echo The Unity executable path %UNITY_PATH% does not exist.
    exit /b
)

set CASE=%1
set DIRECTION=%2
set RMAX=%3
set FRAMES=%4
set LOG_SCREENSHOTS=%5
set LOG_ATTACK=%6
set LOG_RTT=%7

%UNITY_PATH% -projectPath %PROJECT_PATH% -executeMethod %METHOD_NAME% "-single" %CASE% %DIRECTION% %RMAX% 0 %FRAMES% %LOG_SCREENSHOTS% %LOG_ATTACK% %LOG_RTT% 
