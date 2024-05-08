using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using Attack;
using Attack.Cases;
using Orbits;
using Orbits.Satellites;
using Routing;
using UnityEngine;
using Scene;
using UnityEditor;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utilities;
using Debug = UnityEngine.Debug;
using Path = System.IO.Path;

public enum LogChoice { None, RTT, Distance, HopDists, LaserDists, Path };

public class Main : MonoBehaviour
{
	[Header("Environment Parameters")]
	// TODO: Add headers and spaces here.
	public CustomCamera cam;
	
	[Tooltip("Speed 1 is realtime")]
	public float speed = 1f; // a value of 1 is realtime
	float simspeed; // actual speed scaled appropriately

	[Tooltip("Enable use of inter-sat lasers")]
	public bool use_isls = true;
	[HideInInspector]
	public float direction = 1f;
	[HideInInspector]
	public float directionChangeSpeed = 2f;

	public float attack_radius = 1000f;

	public bool captureMode = true;
	private Captures captures;
	const double earthperiod = 86400f;

	public GameObject orbit;

	private Attacker _attacker;
	public GameObject satellite;
	public GameObject laser;
	public GameObject thin_laser;
	public GameObject city_prefab;
	public GameObject beam_prefab;
	public GameObject beam_prefab2;

	private Dictionary<int, List<int>> orbit2sats = new Dictionary<int, List<int>>(); /* Orbit ID mapping to satellite ID list. */

	private LinkCapacityMonitor _link_capacities; /* (node1, node2) link mapping to capacity. Links are full duplex. */

	private CityCreator _city_creator;
	GameObject[] orbits;
	double[] orbitalperiod;
	Satellite[] satlist;
	Vector3[] orbitaxes;
	int orbitcount = 0, satcount = 0;
	public Material isl_material;

	public Material[] laserMaterials;

	public Material[] targetLinkMaterial; // 1st is link up, 2nd is link down.
	public Material cityMaterial;
	[FormerlySerializedAs("countdown")] public Text leftbottom;
	public Text rightbottom;
	
	float elapsed_time;
	float last_speed_change;
	float last_elapsed_time;
	int framecount = 0;
	int maxsats; // total number of satellites
	int phase1_sats;  // number of satellites in phase 1
	int maxorbits;
	const int maxlasers = 4;
	int isl_plane_shift = 0;
	int isl_plane_step = 0;
	bool isl_connect_plane = true;
	int isl_plane2_shift = 0;
	int isl_plane2_step = 0;
	float pause_start_time;
	Node[] nodes;
	ScenePainter _painter;
	float km_per_unit;
	private ConstellationContext constellation_ctx;
	private StreamWriter _fileWriter;
	private StreamWriter _pathLogger;
	private readonly GroundstationCollection groundstations = new GroundstationCollection();
	private RouteHandler _routeHandler;
	GameObject[] lasers;
	int lastpath; // used for multipaths
	Satellite[] nearest_sats;  // used for calculating collision distances
	StreamWriter logfile;
	float maxdist = 0f;
	float beam_radius = 0f;
	float margin = 100f;

	private string _loggingDirectory;

	[Header("Attack Parameters")]
	[FormerlySerializedAs("qualitativeCase")] [FormerlySerializedAs("attackArea")] [FormerlySerializedAs("attack_choice")] public CaseChoice caseChoice;
	public Direction targetLinkDirection;
	[Space]
	
	int decimator = 1;
	float raan0 = 0f;

	// Deprecated log choice.
	LogChoice log_choice = LogChoice.None;

	// TODO: is this ever used?
	public enum BeamChoice { AllOff, AllOn, SrcDstOn };
	
	// TODO: is this ever used?
	public BeamChoice beam_on;
	
	// TODO: is this ever used?
	public bool graph_on;

	[Header("Defence Parameters")]
	public bool defence_on;
	
	void Start()
	{
		// handle time in a way that copes with speed change mid sim
		last_elapsed_time = Time.time;
		last_speed_change = last_elapsed_time;
		elapsed_time = 0f;

		orbitcount = 0;
		satcount = 0;
		Application.runInBackground = true;
		
		// Create cities
		_city_creator = new CityCreator(transform, city_prefab, groundstations);
		_city_creator.DefaultCities();
		

		/* ask the camera to view the same area as our route */
		CustomCamera camscript = (CustomCamera)cam.GetComponent(typeof(CustomCamera));
		camscript.SetupView();
		AttackerParams attackerParams = CasesFactory.GetCase(caseChoice, _city_creator, targetLinkDirection, groundstations, camscript).GetParams();
		camscript.InitView();
		//

		// Set the constellation.
		maxorbits = 24 / decimator;
		int satsperorbit = 66;				// shell altitude
		float sat0alt = 550f;
		int beam_angle = 25;				// angle above horizon sat is reachable (likely 25 or 40 degrees)
		maxdist = 1123f;					// max RF distance from sat to ground station
		beam_radius = 940f;
		int orbital_period = 5739;			// seconds
		isl_connect_plane = true;
		isl_plane_shift = -1;				// isl offset to next plane
		isl_plane_step = 1;
		float phase_offset = 13f / 24f;
		maxsats = maxorbits * satsperorbit;
		phase1_sats = maxsats;				// will differ if simulating multiple phases
		simspeed = speed * 360f / 86400f;	// Speed 1 is realtime
		rightbottom.text = "";		

		orbits = new GameObject[maxorbits];
		orbitalperiod = new double[maxorbits];
		orbitaxes = new Vector3[maxorbits];

		/* new model */
		satlist = new Satellite[maxsats];
		const float earth_r = 6371f; // earth radius
		float sat0r = sat0alt + earth_r;  // sat radius from earth centre
					// beam_angle, beam_radius);
		CreateSats(maxorbits, satsperorbit, 53f, 0f, 0f, phase_offset, orbital_period, sat0r,
					beam_angle, beam_radius);
		float earthdist = Vector3.Distance(satlist[0].gameobject.transform.position, transform.position);
		km_per_unit = sat0r / earthdist;  // sim scale factor


		
		// Initialize RouteGraph
		_routeHandler = new RouteHandler(_painter);
		_routeHandler.InitRoute(maxsats,satlist, maxdist, km_per_unit);

		Debug.Assert(satcount == maxsats);

		// Create satellites
		for (int satnum = 0; satnum < maxsats; satnum++)
		{
			satlist[satnum].glow = false;
			for (int s2 = 0; s2 < maxsats; s2++)
			{
				if (satnum != s2)
				{
					satlist[satnum].AddSat(satlist[s2]);
				}
			}
		}
		
		// Draw existing links between satellites.
		for (int satnum = 0; satnum < maxsats; satnum++)
		{
			if (use_isls)
			{
				if (isl_connect_plane)
				{
					// connect lasers along orbital plane
					satlist[satnum].PreAssignLasersOrbitalPlane();
				}
				else
				{
					satlist[satnum].PreAssignLasersBetweenPlanes(isl_plane2_shift, isl_plane2_step);
				}
				satlist[satnum].PreAssignLasersBetweenPlanes(isl_plane_shift, isl_plane_step);
			}
		}
		Thread.Sleep(10000);	// give the program enough time to generate all game objects.

		// attacker environment set up
		_painter = new ScenePainter(isl_material, laserMaterials, targetLinkMaterial, cityMaterial);
		_link_capacities = new LinkCapacityMonitor();
		
		// Set up logging
		_loggingDirectory = $"{Directory.GetCurrentDirectory()}/Logs/Captures/{caseChoice}_{targetLinkDirection}";
		InitLoggingDirectory();
		if (captureMode)
		{
			CreateFrameScreenshotter();
		}
		CreateFrameLogger();
		CreatePathLogger();
		
		// Create attacker entity.
		_attacker = new Attacker(attackerParams, sat0r, attack_radius, transform, city_prefab, groundstations, _routeHandler, _painter, _link_capacities, _fileWriter, _pathLogger);

		CreateContext();
	}

	/// <summary>
	/// Reinitialize the logging directory.
	/// </summary>
	void InitLoggingDirectory()
	{
		if (Directory.Exists(_loggingDirectory))
		{
			Directory.Delete(_loggingDirectory, true);
		}
		Directory.CreateDirectory(_loggingDirectory);
	}

	/// <summary>
	/// Create logger to record information about the frames.
	/// </summary>
	void CreateFrameLogger()
	{
		string path = Path.Combine(_loggingDirectory, $"{caseChoice}_{targetLinkDirection}.csv");
		_fileWriter = new StreamWriter(path);
		_fileWriter.WriteLine("FRAME,TARGET LINK,ROUTE COUNT,FINAL CAPACITY"); 
	}

	/// <summary>
	/// Create logger to save a screenshot of each frame.
	/// </summary>
	void CreateFrameScreenshotter()
	{
		captures = new Captures(_loggingDirectory, $"{caseChoice}_{targetLinkDirection}");
	}
	
	/// <summary>
	/// Create logger to record information about the attack routes selected for each frame.
	/// </summary>
	void CreatePathLogger()
	{
		string path = Path.Combine(_loggingDirectory, "paths.csv");
		_pathLogger = new StreamWriter(path);
		_pathLogger.WriteLine("FRAME,PATHS");
	}

	/// <summary>
	/// Create any context objects tied to the code.
	/// </summary>
	void CreateContext()
	{		
		// Constellation context.
		constellation_ctx = new ConstellationContext
		{
			satlist = satlist,
			km_per_unit = km_per_unit,
			maxsats = maxsats,
			maxdist =  maxdist,
			margin = margin,
		};
	}
	
	/// <summary>
	/// Default way to create a constellation
	/// </summary>
	void CreateSats(int num_orbits, int sats_per_orbit, float inclination, float orbit_phase_offset,
		float sat_phase_offset, float sat_phase_stagger, double period, float altitude,
		int beam_angle, float beam_radius)
	{
		float orbit_angle_step = 360f / num_orbits;
		for (int i = 0; i < num_orbits; i++)
		{
			orbits[orbitcount] = (GameObject)Instantiate(orbit, transform.position, transform.rotation);
			orbits[orbitcount].transform.RotateAround(Vector3.zero, Vector3.forward, inclination);
			float orbit_angle = orbit_phase_offset * orbit_angle_step + i * orbit_angle_step + raan0;
			orbits[orbitcount].transform.RotateAround(Vector3.zero, Vector3.up, orbit_angle);
			orbitaxes[orbitcount] = Quaternion.Euler(0, orbit_angle, 0) * (Quaternion.Euler(0, 0, inclination) * Vector3.up);
			orbitalperiod[orbitcount] = period;
			orbits[orbitcount].transform.localScale = new Vector3(altitude, altitude, altitude);
			for (int s = 0; s < sats_per_orbit; s++)
			{
				double sat_angle_step = 360f / sats_per_orbit;
				double sat_angle = (-1f * sat_phase_offset * sat_angle_step) + (i * sat_angle_step * sat_phase_stagger) + (s * sat_angle_step);
				Satellite newsat = new Satellite(satcount, s, i, transform, orbits[orbitcount],
					sat_angle, maxlasers, maxsats, phase1_sats, sat_phase_stagger, sats_per_orbit, num_orbits,
					altitude, beam_angle, beam_radius, satellite, beam_prefab, beam_prefab2, laser, thin_laser,
					logfile, log_choice);
				satlist[satcount] = newsat;
				
				// Add the satellite to the orbit's list of satellites.
				if (!orbit2sats.ContainsKey(i))
				{
					orbit2sats.Add(i, new List<int>());
				}
				orbit2sats[i].Add(newsat.satid);

				if (beam_on == BeamChoice.AllOn)
				{
					newsat.BeamOn();
				}
				satcount++;


			}
			OrbitScript os = (OrbitScript) orbits[orbitcount].GetComponent(typeof(OrbitScript));
			os.orbit_id = i;
			
			orbitcount++;
		}
	}
	
	// Only uncomment for debugging if I need to see the attack sphere.
	void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(_attacker.Target.Center, _attacker.Target.Radius);
	}

	void RotateCamera()
	{
		// Update the direction of the orbits
		if (direction < 1f)
		{
			direction += Time.deltaTime / (directionChangeSpeed / 2);
		}
		
		// Move the orbits
		for (int i = 0; i < maxorbits; i++)
		{
			orbits[i].transform.RotateAround(Vector3.zero, orbitaxes[i], (float)(-1f * earthperiod / orbitalperiod[i] * simspeed * direction) * Time.deltaTime);
		}
	}

	/// <summary>
	/// Update function to run the main routegraph rebuild + attacker + frame recording (if enabled)
	/// </summary>
	void Update()
	{
			// TODO: DEMO MODE NEEDS TO PAUSE FOR EACH STEP/TAKE A SCREENSHOT.
			// Update scene initial parameters
			elapsed_time = last_elapsed_time + (Time.time - last_speed_change) * speed;
			RotateCamera();

			// Reset the link capacities. 
			_link_capacities.Reset();

			// Clear the scene.
			RouteHandler.ClearRoutes(_painter);

			// Start logging for this frame.
			_fileWriter.Write($"{framecount}");
			_pathLogger.Write($"{framecount}");

			// Attempt an attack.
			_attacker.Run(constellation_ctx, graph_on, groundstations.ToList());

			// Finish logging for this frame.
			_fileWriter.Write($"\n");
			_pathLogger.Write("\n");
			_fileWriter.Flush();
			_pathLogger.Flush();

			// Update the scene.
			_painter.UpdateLasers(satlist, maxsats, speed);
			leftbottom.text = $"Frame {framecount}";

			// Take a screenshot (if captureMode is enabled)
			if (captureMode)
			{
				captures.CaptureState(cam, leftbottom, framecount); //, leftbottom);
			if (framecount == 50)
			{
				Terminate();
			}
			}


			framecount++;
	}

	private void Terminate()
	{
		if (captureMode)
		{
			SaveVideo();
		}
		PlotData();
		EditorApplication.Exit(0);
	}

	private void SaveVideo()
	{
		int imgHeight = 748 * cam.cam_count;
		int imgWidth = 1504;

		string command =
			// $"ffmpeg -framerate 3 -i {Directory.GetCurrentDirectory()}/Logs/Captures/{_loggingDirectory}/{qualitativeCase}_{targetLinkDirection}_%02d.png -vf \"scale={imgWidth}:{imgHeight}\" -c:v libx265 -preset fast -crf 20 -pix_fmt yuv420p {Directory.GetCurrentDirectory()}/Logs/Captures/{_loggingDirectory}/output.mp4";
			$"ffmpeg -framerate 3 -i {_loggingDirectory}/{caseChoice}_{targetLinkDirection}_%02d.png -vf \"scale={imgWidth}:{imgHeight}\" -c:v libx265 -preset fast -crf 20 -pix_fmt yuv420p {_loggingDirectory}/output.mp4";
		ExecutePowershellCommand(command);
	}

	private void PlotData()
	{

		string command = $"python generate_graph.py {_loggingDirectory}/{caseChoice}_{targetLinkDirection}.csv {_loggingDirectory}/{caseChoice}_{targetLinkDirection}_graph.svg";
		ExecutePowershellCommand(command);
	}

	private void ExecutePowershellCommand(string command)
	{
		// TODO
		ProcessStartInfo startInfo = new ProcessStartInfo()
		{
			FileName = "powershell.exe",
			Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
			UseShellExecute = false,
			RedirectStandardOutput = false,
			RedirectStandardError = true,
			CreateNoWindow = true 
		};

		Process process = new Process() { StartInfo = startInfo };

		process.Start();
		string errors = process.StandardError.ReadToEnd();

		if (!process.WaitForExit(5000)) // Kills the process after 5 seconds.
		{
			process.Kill();
		}
		process.Close();
		
		if (!string.IsNullOrEmpty(errors))
		{
			Debug.LogError("PowerShell Errors: " + errors);
		}
	}
}