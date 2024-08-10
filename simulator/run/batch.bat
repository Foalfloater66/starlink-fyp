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

:: Arguments
set FILE=%1

:: Measure time before launching Unity script
echo Started at !TIME!

:: Launch Unity script
%UNITY_PATH% -projectPath %PROJECT_PATH% -executeMethod %METHOD_NAME% "-batch" %FILE%

:: Measure time after Unity script finishes
echo Completed at: !TIME!
