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

:: Read the arguments
set CASE=%1
set DIRECTION=%2
set RMAX=%3
set ID=%4
set FRAMES=%5
set LOG_SCREENSHOTS=%6
set LOG_VIDEO=%7
set LOG_ATTACK=%8
set LOG_RTT=%9
shift
set LOG_HOPS=%9

%UNITY_PATH% -projectPath %PROJECT_PATH% -executeMethod %METHOD_NAME% "-single" %CASE% %DIRECTION% %RMAX% %ID% %FRAMES% %LOG_SCREENSHOTS% %LOG_VIDEO% %LOG_ATTACK% %LOG_RTT% %LOG_HOPS%
