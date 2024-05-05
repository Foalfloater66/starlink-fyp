# Starlink Link Flooding Attack Simulator for Unity
This is a link flooding attack simulator on the SpaceX Starlink network based on [Mark Handley's SpaceX Starlink simulator](https://github.com/mhandley/Starlink0031).

It simulates singular link flooding attacks on inter-satellite network (ISN) links located within a 1000km radius of a specified center point.

## Running the Simulator
_Adapted from Mark Handley's instructions._

To run the simulator, use version 2019.2.1f1 of Unity. 
Clone the GitHub repository, and from UnityHub, add the repository directory as a new project.

Select the project from UnityHub and Unity should start.

In the Project tab near to the bottom of the screen, open the  `Assets/Orbits` folder, and select `Scene_SP_basic`.

You should be able to run a simulation using the Play icon at the top of the screen.

To select simulation parameters in the object Inspector, select the `EarthHigh` object on the left.

Note that this program requires high computational effort from your device and may have difficulties starting up.
If Unity becomes unresponsive, please follow the instructions in [Troubleshooting](#troubleshooting).

## Simulator Output Files
Given a `qualitativeCase` and `targetLinkOrientation`, running this code will result in the creation of a `Logs/Captures/{qualitativeCase}_{targetLinkOrientation}` directory.

Additionally, running this code with `captureMode` enabled will save each frame to disk under the name `{qualitativeCase}_{targetLinkOrientation}_{framecount}.png` and generate an `output.mp4` video in the same folder.
To avoid saving too many large files, this mode only runs for the first 50 frames.

Below is an example directory listing for parameters `qualitativeCase=Landlocked` and `targetLinkOrientation=East`.

```bash
├───Logs
│   └───Captures
│       ├───Landlocked_East
│               Landlocked_East.csv          # Logs the target link name, attack route count, and final target link capacity for each frame.
│               Landlocked_East_*.png        # Image combining every camera view of the scene under a frame (if captureMode is enabled)
│               Landlocked_East_graph.svg    # Graph summarizing information from Landlocked_East.csv
│               output.mp4                   # Compiled video of the frames (if captureMode is enabled)
│               paths.csv                    # Logs the attack paths for each frame.
```
## Link Status Color Coding
The simulation uses the following color coding scheme:

| Color | Meaning |
| - | - |
| orange | Used radio frequency link |
| green | Used ISN link |
| purple | Unused ISN link |
| red | Congested target ISN link |
| light pink/white | Uncongested target ISN link |

Below are some example frames.
These can be reproduced by using parameters `qualitativeCase=Landlocked` and `targetLinkDirection=East`.

![Landlocked_East_03](https://github.com/Foalfloater66/starlink-fyp/assets/72133888/579219cf-f900-4b38-9c2a-96f5824a28ef)
Example congested target link.

![Landlocked_East_08](https://github.com/Foalfloater66/starlink-fyp/assets/72133888/af506c12-7e7c-4c56-a769-7e495d62880f)
Example uncongested target link.

## Troubleshooting
When enabling `captureMode`, screenshots of the scene are taken from each camera enabled in the `qualitativeCase` and saved to disk at each frame. 
More precisely, the combined creation of city game objects and forced scene rendering at the first frame requires a high number of computations within a small timeframe and can cause the Unity simulation to become unresponsive.
If this does occur, there are a couple of possible venues which you may apply individually or together.

For reference, the experiments were run on a Windows device equipped with an Intel Core i9 CPU and NVIDIA GeForce RTX 3070 Ti laptop GPU.
Devices running this simulation with a low-performance CPU or one whose architecture is not supported by Unity 2019.2.1f1 (e.g, Apple M* chips which use delay-inducing Rosetta ISA translation) are more likely to encounter this issue.
### Enable Debugging Mode
From a Unity-supporting IDE like Rider or Visual Studio, add a breakpoint to the [`Main.Update()`](https://github.com/Foalfloater66/starlink-fyp/blob/4881396f83662f559eaf89ddb3e5df7abeb6d089/Assets/Main.cs#L333) method.
Then, attach the Unity editor and run the program in debugging mode with breakpoints enabled.
If the program remains responsive after the first couple of executions of `Main.Update()`, disable the breakpoints.
The program should now run as intended.

### Save the Unity Scene Under Different Parameters
Select a set of `qualitativeCase`and `targetLinkDirection`parameters that you _do not intend to run_.
Save the Unity scene.
Then, change the parameters to the intended parameters and run the program. _Do not save the Scene before running the program_.

