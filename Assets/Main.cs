using System;
using System.Collections.Generic;
using System.Linq;
using Attack;
using Orbits;
using Orbits.Satellites;
using Routing;
using UnityEngine;
using Scene;
using Scene.GameObjectScripts;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Utilities;

// TODO: remove the below as well.
public enum LogChoice { None, RTT, Distance, HopDists, LaserDists, Path };

public struct MainContext
{
	// REVIEW: RENAME MAIN TO STARLINK? UNSURE? MODEL?
	public ScenePainter painter;
	public LinkCapacityMonitor linkCapacityMonitor;
	public RouteGraph routeGraph;
	public RouteHandler routeHandler;
}
public class Main : MonoBehaviour
{
	public CustomCamera cam;
	[Tooltip("Spin: Yes or No")]
	public bool spin;
	[Tooltip("Speed 1 is realtime")]
	public float speed = 1f; // a value of 1 is realtime
	float orig_speed = 1f;
	float simspeed; // actual speed scaled appropriately

	[Tooltip("Enable use of inter-sat lasers")]
	public bool use_isls = true;
	[Tooltip("Enable use of ground relays")]
	public float relay_dist_step = 1f;
	[HideInInspector]
	public float direction = 1f;
	[HideInInspector]
	public float directionChangeSpeed = 2f;

	public float attack_radius = 800f;

	public bool debug = false;
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
	public int initial_link_capacity = 1000;

	GameObject[] orbits;
	double[] orbitalperiod;
	Satellite[] satlist;
	Vector3[] orbitaxes;
	int orbitcount = 0, satcount = 0;
	public Material isl_material;

	public Material yellowMaterial; // CLEANUP: where is this? what is this used for? I need to move this to the ScenePainter. I don't seem to be using this anywhere. I should be able to remove this.
	public Material[] laserMaterials;

	public Material[] targetLinkMaterial; // 1st is link up, 2nd is link down.
	public Material cityMaterial;
	public Text txt;
	public Text countdown;
	public Text rightbottom;
	float elapsed_time;
	float last_speed_change;
	float last_elapsed_time;
	int framecount = 0;
	int maxsats; // total number of satellites
	int phase1_sats;  // number of satellites in phase 1
	public int maxorbits;
	const int maxlasers = 4;
	int isl_plane_shift = 0;
	int isl_plane_step = 0;
	bool isl_connect_plane = true;
	int isl_plane2_shift = 0;
	int isl_plane2_step = 0;
	double meandist;
	float start_time;
	bool pause = false;
	float pause_start_time;
	Node[] nodes;
	// RouteGraph rg; // CLEANUP: change with underscore in front 
	ScenePainter _painter;
	float km_per_unit;

	private ConstellationContext constellation_ctx;
	private MainContext ctx;

	private readonly GroundstationCollection groundstations = new GroundstationCollection();

	/*
	 * TODO for groundstation support:
	 * 1. Extract a desired set of groundstations by name.
	 * 2. Extract a sample number of groundstations.
	 */

	private RouteHandler routeHandler;

	//GameObject beam1 = null, beam2 = null;
	GameObject[] lasers;
	List<GameObject> relays;
	string prevpath = "";

	bool route_init = false;
	Vector3 sat0pos;

	int lastpath; // used for multipaths

	int followsat_id = 0;
	float last_dist_calc = -1000f; // last time we update the set of nearest routers
	Satellite[] nearest_sats;  // used for calculating collision distances
	float mindist = Node.INFINITY; // used for calculating collision distances

	System.IO.StreamWriter logfile;
	System.IO.StreamWriter summary_logfile;
	float maxdist = 0f;
	float beam_radius = 0f;
	float margin = 100f;
	long elapsed_sum = 0;
	int elapsed_count = 0;

	public AttackChoice attack_choice;
	
	public enum ConstellationChoice { P24_S66_A550, P72_S22_A550, P32_S50_A1100 };
	public ConstellationChoice constellation;
	public int no_of_paths = 1;

	public int decimator;
	public float raan0 = 0f;

	public LogChoice log_choice = LogChoice.None;

	public static string log_directory = "/Users/morganeohlig/workspace/fyp/Starlink0031/Logs/";

	public enum BeamChoice { AllOff, AllOn, SrcDstOn };
	public BeamChoice beam_on;
	public bool graph_on;

	public Utilities.SceneField prevscene;
	public Utilities.SceneField nextscene;

	void Start()
	{
		// handle time in a way that copes with speed change mid sim
		last_elapsed_time = Time.time;
		last_speed_change = last_elapsed_time;
		elapsed_time = 0f;

		orbitcount = 0;
		satcount = 0;
		Application.runInBackground = true;
		sat0pos = Vector3.zero; // center of earth!

		// Create logging objects
		switch (log_choice)
		{
			case LogChoice.RTT:
				logfile = new System.IO.StreamWriter(log_directory + "/rtt.txt");
				break;
			case LogChoice.Distance:
				logfile = new System.IO.StreamWriter(log_directory + "/distance.txt");
				break;
			case LogChoice.HopDists:
				logfile = new System.IO.StreamWriter(log_directory + "/hop_dists.txt");
				break;
			case LogChoice.LaserDists:
				logfile = new System.IO.StreamWriter(log_directory + "laser_dists.txt");
				break;
			default: // for LogChoice.None and LogChoice.Path
				break;
		}
		summary_logfile = new System.IO.StreamWriter(log_directory + "summary.txt");

		start_time = Time.time;

		/* ask the camera to view the same area as our route */
		CustomCamera camscript = (CustomCamera)cam.GetComponent(typeof(CustomCamera));
		// camscript.route_choice = route_choice;
		camscript.attack_choice = attack_choice;
		camscript.InitView();

		int satsperorbit = 0;
		float sat0alt = 0;  // altitiude of sat 0 (other shells may have different altitudes)
		int beam_angle = 0;  // angle above horizon sat is reachable (likely 25 or 40 degrees)
		int orbital_period = 0;
		float phase_offset = 0f;

		// Choose a constellation.
		switch (constellation)
		{
			case ConstellationChoice.P24_S66_A550:
				// phase 1 from 2019 FCC filing
				maxorbits = 24 / decimator;
				satsperorbit = 66;
				sat0alt = 550f;
				beam_angle = 25;
				maxdist = 1123f; // max RF distance from sat to ground station
				beam_radius = 940f;
				orbital_period = 5739; // seconds
				isl_connect_plane = true;
				isl_plane_shift = -1;  // isl offset to next plane
				isl_plane_step = 1;
				phase_offset = 13f / 24f;
				break;
			case ConstellationChoice.P72_S22_A550:
				// phase 1 from 2018 FCC filing
				maxorbits = 72 / decimator;
				satsperorbit = 22;
				sat0alt = 550f;
				beam_angle = 25;
				maxdist = 1123f;
				beam_radius = 940f;
				orbital_period = 5739; // seconds	
				isl_connect_plane = false;
				isl_plane_shift = -1;
				isl_plane_step = 1;
				isl_plane2_shift = -1;
				isl_plane2_step = 2;
				phase_offset = 39f / 72f;
				break;
			case ConstellationChoice.P32_S50_A1100:
				// phase 1 from 2016 FCC filing
				maxorbits = 32 / decimator;
				satsperorbit = 50;
				sat0alt = 1150f;
				beam_angle = 40;
				maxdist = 1600f;  // rough guess 1150*sqrt(2), but check this value
				beam_radius = 1060f;
				orbital_period = 6500; // seconds
				isl_connect_plane = true;
				isl_plane_shift = 0;
				isl_plane_step = 1;
				phase_offset = 11f / 32f;
				break;
		}
		maxsats = maxorbits * satsperorbit;
		phase1_sats = maxsats;  // will differ if simulating multiple phases

		orig_speed = speed;
		simspeed = speed * 360f / 86400f; // Speed 1 is realtime
		rightbottom.text = speed.ToString() + "x realtime";

		orbits = new GameObject[maxorbits];
		orbitalperiod = new double[maxorbits];
		orbitaxes = new Vector3[maxorbits];

		/* new model */
		satlist = new Satellite[maxsats];

		const float earth_r = 6371f; // earth radius
		float sat0r = sat0alt + earth_r;  // sat radius from earth centre

		// More constellation configurations
		switch (constellation)
		{
			case ConstellationChoice.P24_S66_A550:
				CreateSats(maxorbits, satsperorbit, 53f, 0f, 0f, phase_offset, orbital_period, sat0r,
					beam_angle, beam_radius);
				break;

			case ConstellationChoice.P72_S22_A550:
				CreateSatsDirect(maxorbits, satsperorbit, 53f, 0f, phase_offset,
					decimator * phase_offset * 360f / 22f,
					decimator * 360f / 72f, 360 / 22f, orbital_period, sat0r,
					beam_angle, beam_radius);
				break;

			case ConstellationChoice.P32_S50_A1100:
				CreateSats(maxorbits, satsperorbit, 53f, 0f, 0f, phase_offset, orbital_period, sat0r,
					beam_angle, beam_radius);
				break;
		}

		float earthdist = Vector3.Distance(satlist[0].gameobject.transform.position, transform.position);
		km_per_unit = sat0r / earthdist;  // sim scale factor

		// Create cities
		InitCities();
		
		// Create and initialise the RouteGraph
		routeHandler = new RouteHandler(_painter);
		routeHandler.InitRoute(maxsats,satlist, relays, maxdist, km_per_unit);

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
		meandist = 0;
		for (int satnum = 1; satnum < maxsats; satnum++)
		{
			meandist += Vector3.Distance(satlist[0].position(), satlist[satnum].position());
		}
		meandist /= (maxsats - 1);
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

		_painter = new ScenePainter(isl_material, laserMaterials, targetLinkMaterial, cityMaterial);

		_link_capacities = new LinkCapacityMonitor(initial_link_capacity);
		
		// TODO: instead, make this return the target link!
		// Get target link + list of source groundstations.
		// AttackCases.getTargetLink();
		AttackCases.getTargetCoordinates(attack_choice, out float target_lat, out float target_lon);
		AttackCases.getSourceGroundstations(attack_choice, groundstations, out List<GameObject> src_groundstations);
		
		// Create attacker entity.
		_attacker = new Attacker(target_lat, target_lon, sat0r, attack_radius, src_groundstations, transform, city_prefab, groundstations, routeHandler, _painter, _link_capacities);

		createContext();
	}

	/// <summary>
	/// Create any context objects tied to the code.
	/// TODO: create a context after each section.
	/// </summary>
	void createContext()
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
	/// Create cities and place them on planet Earth.
	/// </summary>
	void InitCities()
	{
		// N and W are +ve
		_city_creator = new CityCreator(transform, city_prefab, groundstations);
		relays = new List<GameObject>(); // TODO: remove this.
		
		switch (attack_choice)
		{
			case AttackChoice.Demo:
			case AttackChoice.CoastalUS:
			case AttackChoice.TranscontinentalUS:
			case AttackChoice.Polar:
				_city_creator.NACities();
				break;
			case AttackChoice.Equatorial:
				// TODO: add cities. (US + SA)
				_city_creator.USACities();
				break;
			case AttackChoice.IntraOrbital:
				// TODO: add cities. (find a path too)
			case AttackChoice.TransOrbital:
				// TODO: add cities. (find a path too)
				break;
		}
	}
	
	/// <summary>
	/// Default way to create a constellation
	/// </summary>
	/// <param name="num_orbits"></param>
	/// <param name="sats_per_orbit"></param>
	/// <param name="inclination"></param>
	/// <param name="orbit_phase_offset"></param>
	/// <param name="sat_phase_offset"></param>
	/// <param name="sat_phase_stagger"></param>
	/// <param name="period"></param>
	/// <param name="altitude"></param>
	/// <param name="beam_angle"></param>
	/// <param name="beam_radius"></param>
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
			orbitcount++;
		}
	}
	
	/// <summary>
	/// Alternative way to create a constellation (used for August 2019 constellation, as its high inter-plane
	/// phase offset and high number of orbits cause wrapping with CreateSats)
	/// </summary>
	/// <param name="num_orbits"></param>
	/// <param name="sats_per_orbit"></param>
	/// <param name="inclination"></param>
	/// <param name="orbit_phase_offset"></param>
	/// <param name="sat_phase_offset"></param>
	/// <param name="sat_angle_stagger"></param>
	/// <param name="orbit_angle_step"></param>
	/// <param name="sat_angle_step"></param>
	/// <param name="period"></param>
	/// <param name="altitude"></param>
	/// <param name="beam_angle"></param>
	/// <param name="beam_radius"></param>
	void CreateSatsDirect(int num_orbits, int sats_per_orbit, float inclination, float orbit_phase_offset,
		float sat_phase_offset, float sat_angle_stagger /* degrees */,
		float orbit_angle_step, float sat_angle_step, double period, float altitude,
		int beam_angle, float beam_radius)
	{

		for (int i = 0; i < num_orbits; i++)
		{
			orbits[orbitcount] = (GameObject)Instantiate(orbit, transform.position, transform.rotation);
			orbits[orbitcount].transform.RotateAround(Vector3.zero, Vector3.forward, inclination);
			float orbit_angle = -1 * (orbit_phase_offset * orbit_angle_step + i * orbit_angle_step) + raan0;
			orbits[orbitcount].transform.RotateAround(Vector3.zero, Vector3.up, orbit_angle);
			orbitaxes[orbitcount] = Quaternion.Euler(0, orbit_angle, 0) * (Quaternion.Euler(0, 0, inclination) * Vector3.up);
			orbitalperiod[orbitcount] = period;
			orbits[orbitcount].transform.localScale = new Vector3(altitude, altitude, altitude);
			if (satcount < maxsats)
			{
				for (int s = 0; s < sats_per_orbit; s++)
				{
					double sat_angle = (-1 * i * sat_angle_stagger) + (-1 * s * sat_angle_step);
					while (sat_angle < sats_per_orbit * -360f / 22f)
					{
						sat_angle += sats_per_orbit * 360f / 22f;
					}
					sat_angle += 90f;
					Satellite newsat =
						new Satellite(satcount, s, i, transform, orbits[orbitcount], sat_angle, maxlasers,
							maxsats, phase1_sats, sat_phase_offset, sats_per_orbit, num_orbits, altitude,
							beam_angle, beam_radius, satellite, beam_prefab, beam_prefab2,
							laser, thin_laser, logfile, log_choice);
					satlist[satcount] = newsat;
					if (beam_on == BeamChoice.AllOn)
					{
						newsat.BeamOn();
					}
					satcount++;
				}
			}
			orbitcount++;
		}
	}
	
	// Only uncomment for debugging if I need to see the attack sphere.
	// void OnDrawGizmos()
	// {
	// 	Gizmos.color = Color.red;
	// 	Gizmos.DrawSphere(_attacker.TargetAreaCenterpoint, _attacker.Radius);
	// }

	void RotateCamera()
	{
		int i = 0;
		if (direction < 1f)
		{
			direction += Time.deltaTime / (directionChangeSpeed / 2);
		}

		if (spin)
		{
			transform.Rotate(-Vector3.up, (simspeed * direction) * Time.deltaTime);
		}
		for (i = 0; i < maxorbits; i++)
		{
			orbits[i].transform.RotateAround(Vector3.zero, orbitaxes[i], (float)(-1f * earthperiod / orbitalperiod[i] * simspeed * direction) * Time.deltaTime);
		}
	}

	// Update is called once per frame
	void Update()
	{
		Debug.Log("Update Iteration()");
		
		// Update scene initial parameters
		elapsed_time = last_elapsed_time + (Time.time - last_speed_change) * speed;
		RotateCamera();
		
		// Reset the link capacities. 
		_link_capacities.Reset();
		
		// Clear the scene.
		RouteHandler.ClearRoutes(_painter);
		
		// Attempt an attack.
		_attacker.Run(constellation_ctx, graph_on, groundstations.ToList());
		Debug.Log("After running the attacker object.");
		
		// Update the scene.
		_painter.UpdateLasers(satlist, maxsats, speed);
		Debug.Log("After Updating the Lasers");
		
		framecount++;
	}
}