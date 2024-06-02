@echo off
:: Manually set the Unity executable path
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2019.2.1f1\Editor\Unity.exe"
echo UNITY_PATH: %UNITY_PATH%
set PROJECT_PATH=%cd%
echo PROJECT_PATH: %PROJECT_PATH%
set METHOD_NAME="QuickPrimitives.Editor.CommandLineScript.Run"
echo 

@REM TODO: Add command lines args! best to do case set manually. Everything takes too long.

setlocal enabledelayedexpansion

:: Check if the Unity executable exists
if not exist %UNITY_PATH% (
    echo The Unity executable path %UNITY_PATH% does not exist.
    exit /b
)

:: Accepted values for CASE: Landlocked, Coastal, Insular, Polar, Equatorial, TransOrbital, IntraOrbital
set CASE=%1

:: Defined parameters
set DIRECTION_SET=East West North South
set RMAX_SET=1 3 6 9

:: Iterate over the parameter sets
for %%d in (%DIRECTION_SET%) do (
    for %%r in (%RMAX_SET%) do (
        if %%r == 1 (
            echo Case: %CASE%, Direction: %%d, Defence: OFF
            %UNITY_PATH% -projectPath %PROJECT_PATH% -executeMethod %METHOD_NAME% %CASE% %%d "False" %%r 0
        ) else (
            echo Case: %CASE%, Direction: %%d, Rmax: %%r
            :: Run the Unity simulation 30 times for each parameter set
            for /L %%j in (1,1,20) do (
                echo Iteration %%j
                %UNITY_PATH% -projectPath %PROJECT_PATH% -executeMethod %METHOD_NAME% %CASE% %%d "True" %%r %%j
            )
        )
    )
)
