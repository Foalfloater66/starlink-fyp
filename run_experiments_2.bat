@echo off
:: Manually set the Unity executable path
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2019.2.1f1\Editor\Unity.exe"
echo UNITY_PATH: %UNITY_PATH%
set PROJECT_PATH=%cd%
echo PROJECT_PATH: %PROJECT_PATH%
set METHOD_NAME="QuickPrimitives.Editor.CLIScript.Run"
echo 

@REM TODO: Add command lines args! best to do case set manually. Everything takes too long.

setlocal enabledelayedexpansion

:: Check if the Unity executable exists
if not exist %UNITY_PATH% (
    echo The Unity executable path %UNITY_PATH% does not exist.
    exit /b
)

%UNITY_PATH% -projectPath %PROJECT_PATH% -executeMethod %METHOD_NAME%

:: Accepted values for CASE: Landlocked, Coastal, Insular, Polar, Equatorial, TransOrbital, IntraOrbital
@REM set CASE=%1

@REM :: Defined parameters
@REM set DIRECTION_SET=East West North South
@REM set RMAX_SET=1 3 6 9

:: Iterate over the parameter sets
@REM for %%d in (%DIRECTION_SET%) do (
@REM     for %%r in (%RMAX_SET%) do (
@REM         if %%r == 1 (
@REM             echo Case: %CASE%, Direction: %%d, Defence: OFF
@REM             %UNITY_PATH% -projectPath %PROJECT_PATH% -executeMethod %METHOD_NAME% %CASE% %%d "False" %%r 0
@REM         ) else (
@REM             echo Case: %CASE%, Direction: %%d, Rmax: %%r
@REM             :: Run the Unity simulation 30 times for each parameter set
@REM             for /L %%j in (1,1,20) do (
@REM                 echo Iteration %%j
@REM                 %UNITY_PATH% -projectPath %PROJECT_PATH% -executeMethod %METHOD_NAME% %CASE% %%d "True" %%r %%j
@REM             )
@REM         )
@REM     )
@REM )
