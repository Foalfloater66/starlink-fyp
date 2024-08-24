# Starlink Link Flooding Attack Simulator for Unity
This is a link flooding attack simulator on the SpaceX Starlink network based on [Mark Handley's SpaceX Starlink simulator](https://github.com/mhandley/Starlink0031).

It simulates singular link flooding attacks on inter-satellite network (ISN) links located within a 1000km radius of a specified center point.

⚠️ Please note that this program demands high computational effort from your device, and might not function if you do not meet the [system requirements](#system-requirements). 
If the Unity Editor becomes unresponsive, please follow the instructions in [Troubleshooting](#troubleshooting).

Moreover, please note that the simulator this project is based on ignores reachability constraints normally imposed on Starlink's satellites to avoid interfering with geostationary satellite communications.
We chose not to change this, as fixing it would ultimately simply reduce the number of ground stations that the adversary could use near equatorial regions, whereas our main focus is to observe how our mitigation algorithm performs against the attacker. See [Handley's Github repository](https://github.com/mhandley/Starlink0031) for more information.

ℹ️ Since the $i_\text{max}$ parameter was previously named $r_\text{max}$, the code often refers to $r_\text{max}$ instead of $i_\text{max}$. These refer to the same RiDS parameter mentioned in the paper.

## Installation

### System Requirements
This program is most compatible with **Unity 2019.2.1f1** on **Windows 11**.
Due to restrictions from the original code this simulator builds upon, other versions of Unity may experience difficulties at runtime.
Running the code on Windows 10 should be fine.

Due to the high computational demand from this program, we recommend using **gaming devices**, especially those that permit operating mode modification.
For reference, the experiments were run on a ROG Zephyrus M16 equipped with an Intel Core i9 CPU and NVIDIA GeForce RTX 3070 Ti laptop GPU.
Devices running this simulation with a low-performance CPU or a non-Intel architecture (Unity 2019.2.1f1 uses Intel architecture) are more likely to encounter performance issues. 
This includes Apple M* chips which introduce significant delay from Rosetta ISA translation.

_N.B. Recent Unity versions that support M* chips might perform well;
    it simply has not been tested, as it is beyond the scope of this project._


### Unity Scene Setup
1. Clone the repository.
2. From Unity Hub, add the `./simulator` directory as a new project.
3. Go to "File" > "Open Scene" and select `Orbits/Scene_SP_basic`.
   
You're now good to go!

## Running the Simulator

### Through the Unity Editor
_The Unity Editor only allows for single runs. For batch runs, check the [CLI](###cli)._

Simulation parameters can be configured in the object inspector window by pressing `Cmd+3/Ctrl+3` when selecting the `EarthHigh` object.

To run the simulation, simply press `Cmd+P/Ctrl+P` or the Play icon at the top of the screen.

### Through the CLI
_As this simulator was developed on Windows, there are currently no instructions for UNIX devices._

Note that parameters`choice` and `direction`are enums, so understanding their textual translation is crucial to running the simulator correctly. 
These can be found in [CaseChoice.cs](https://github.com/Foalfloater66/starlink-fyp/blob/main/Assets/Attack/Cases/CaseChoice.cs) and [Direction.cs](https://github.com/Foalfloater66/starlink-fyp/blob/main/Assets/Attack/Cases/Direction.cs)

#### Single Run
Singe runs allow you to simulate one ISFLA with desired parameters.
```bat
:: Windows
./simulator/run/single.bat <choice> <direction> <imax> <id> <frames> <log_screenshots> <log_video> <log_attack> <log_rtt> <log_hops>
```

#### Batch Runs
Batch runs allow you to run multiple experiments in one go.

Write an experiments specification in JSON format as shown in [experiments.json](https://github.com/Foalfloater66/starlink-fyp/blob/main/simulator/run/experiments/50frames_20reps_baseline.json):
```json
{
"experiments": [
    {
    "choice": 1,
    "direction": 0,
    "rMax": 1,
    "reps": 1
    }
],
"frames": 100,
"logVideos" : false,
"logScreenshots": false,
"logVideo" : false,
"logAttack": true,
"logRTT": true,
"logHops" : false,
}
```
Each experiment is characterised by its `choice`, `direction`, `rmax`, and number of repetitions `reps`. 
`frames`, `logVideos`, `logScreenshots`, `logVideo` `logAttack`, `logRTT`, and `logHops` are global options that apply to all experiments in the batch run.

Then run:
```bat
:: Windows
./simulator/run/batch.bat <filename>
```
To preserve system file space, we recommend disabling `logScreenshots` and `logVideos` in batch mode.

## Simulation Parameters


#### `choice`
Determines which of the eight qualitative attack examples in the paper should be used: `SimpleDemo`, `Landlocked`, `Coastal`, `Insular`, `Equatorial`, `Polar`, `TransOrbital`, `IntraOrbital`.

Since `SimpleDemo` is meant to show how the simulator works at a lower scale, ISL links in this example are equipped with a maximum capacity of $8$Gbps. 
This is only for demonstrative purposes and not representative of actual ISL capacity.

#### `direction` 
Determines the direction of the target ISL. The "direction" of the target ISL is defined in the paper. The user can choose from `East`, `West`, `North`, `South`, and `Any`.

In `Any`, the first link in the target area is selected regardless of its direction.

#### `rMax`
The $i_\text{max}$ parameter from RiDS, defined in our paper. It must be a non-zero positive integer.

#### `reps`
To number of identical experiments that should be executed consecutively.

### Logging
Below is a list of the logging options that can be enabled and the files they produce.

#### `LogScreenshot`
`frame_<i>.png`: Screenshot of every camera view of a scene at frame `i`.

#### `logVideo`
`output_<i>.mp4`: Recording of the entire simulation from camera `i`.

#### `logAttack`
`attack.csv`: Target link name, attack route count, and final target link capacity for each frame.

`paths.csv`: List of source and destination ground stations for each attack attack path in each frame.

#### `logRTT`
`rtt.json`: List of RTTs of each path in each frame.

#### Log Directory Structure
This is an example directory where all three logging options are enabled.
```bash
├───Logs
│   └───Captures
│       ├───Landlocked_East_OFF_001
|               attack.csv      
│               paths.csv      
|               frame_0.png
|               frame_1.png     
|               frame_2.png
|               frame_3.png
|               frame_4.png
│               output_0.mp4
|               rtt.json
```

## Thesis Experiments
Experimental thesis data was collected using the following command:
```batch
::Windows
./simulator/run/batch.bat ./simulator/run/experiments/50frames_20reps_baseline.json
```

## Troubleshooting
When enabling `logScreenshots`, screenshots of the scene are taken from each enabled camera and saved to disk at each frame. 
More precisely, the combined creation of city game objects and forced scene rendering at the first frame requires a high number of computations within a small timeframe and can cause the Unity simulation to become unresponsive.

If this does occur, please ensure that your computer is not set to an operating mode such as ASUS' ["Turbo" mode](https://rog.asus.com/articles/guides/armoury-crate-performance-modes-explained-silent-vs-performance-vs-turbo-vs-windows/) that attempts to maximize the framerate.
These prioritize high framerate over generating game objects to completion, leading to Unity crashing at the first screenshot.
