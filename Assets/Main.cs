﻿using System;
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
	RouteGraph rg; // CLEANUP: change with underscore in front 
	ScenePainter _painter;
	float km_per_unit;

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
	GroundGrid grid;
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

		grid = new GroundGrid(10, maxdist, margin, km_per_unit, city_prefab, transform);  // 5 degrees is 550km (in lat)
		
		// create cities.
		InitCities();
		
		// Create and initialise the RouteGraph
		rg = new RouteGraph();
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

		// if (route_choice == RouteChoice.Followsat)
		// {
		// 	// We're going to follow a satellite.
		// 	// Need to glue the camera to the satellite.
		// 	followsat_id = 0;
		// 	switch (constellation)
		// 	{
		// 		case ConstellationChoice.P24_S66_A550:
		// 			followsat_id = 78;
		// 			break;
		// 		case ConstellationChoice.P72_S22_A550:
		// 			followsat_id = 1505;
		// 			break;
		// 		case ConstellationChoice.P32_S50_A1100:
		// 			followsat_id = 105;
		// 			break;
		// 	}
		// 	Satellite followsat = satlist[followsat_id];
		// 	cam.transform.position = new Vector3(100, 0, -60);
		// 	cam.transform.rotation = Quaternion.Euler(0, 300, -90);
		// 	cam.transform.SetParent(followsat.gameobject.transform, false);
		// 	followsat.ChangeMaterial(laserMaterials[0]);
		// 	nearest_sats = new Satellite[4];
		// }

		//float earthradius = Vector3.Distance (transform.position, london.gameObject.transform.position);
		//km_per_unit2 = (12756f / 2) / earthradius;
		//Debug.Log("km_per_unit: " + km_per_unit.ToString() + " km_per_unit2: " + km_per_unit2.ToString())

		// Demo list of groundstations.
		List<GameObject> demo_dest_groundstations = new List<GameObject>()
		{
			groundstations["Miami"],
			groundstations["New York"],
			groundstations["Chicago"],
			groundstations["Winnipeg"],
			groundstations["Denver"]
		};
		
		// TODO: Add get target cities too. 
		AttackCases.getTargetCoordinates(attack_choice, out float target_lat, out float target_lon);
		_attacker = new Attacker(target_lat, target_lon, sat0r, attack_radius, demo_dest_groundstations, transform, city_prefab);
		_link_capacities = new LinkCapacityMonitor(initial_link_capacity);
	}

	private void _CANCities()
	{
		// Top 10 most populated Canadian cities as of the 2021 census
		// https://en.wikipedia.org/wiki/List_of_largest_Canadian_cities_by_census
		CreateCity(43.65f, 79.38f, "Toronto");
		CreateCity(45.50f, 73.56f, "Montreal");
		CreateCity(51.04f, 114.07f, "Calgary");
		CreateCity(45.42f, 75.69f, "Ottawa");
		CreateCity(53.54f, 113.49f, "Edmonton");
		CreateCity(49.89f, 97.13f,  "Winnipeg");
		CreateCity(43.58f, 79.64f,  "Mississauga");
		CreateCity(49.28f, 123.12f,  "Vancouver");
		CreateCity(43.73f, 79.76f,  "Brampton");
		CreateCity(43.25f, 79.87f,  "Hamilton");
	}


	private void _USCities()
	{
		// Top 25 US cities as of 2020 census.
		// https://en.wikipedia.org/wiki/List_of_United_States_cities_by_population
		CreateCity(40.76f, 73.98f, "New York");
		CreateCity(34f, 118f,  "Los Angeles"); // Los Angeles
		CreateCity(25.768125f, 80.197006f,  "Miami");
		CreateCity(41.87f, 87.62f,  "Chicago"); //;
		CreateCity(29.76f, 95.36f,  "Houston"); //;
		CreateCity(33.44f, 112.07f,  "Phoenix"); //;
		CreateCity(39.95f, 75.16f,  "Philadelphia");
		CreateCity(29.42f, 98.49f,  "San Antonio"); // Texas
		CreateCity(32.71f, 117.16f,  "San Diego");
		CreateCity(32.46f, 96.47f,  "Dallas");
		CreateCity(30.26f, 97.74f,  "Austin");
		CreateCity(30.33f, 81.65f,  "Jacksonville");
		CreateCity(37.33f, 121.88f,  "San Jose");
		CreateCity(32.75f, 97.33f,  "Fort Worth");
		CreateCity(39.96f, 82.99f,  "Columbus");
		CreateCity(35.22f, 80.84f,  "Charlotte");
		CreateCity(39.76f, 86.15f, "Indianapolis");
		CreateCity(37.77f, 122.41f,  "San Francisco");
		CreateCity(47.60f, 122.33f,  "Seattle");
		CreateCity(39.73f, 104.99f,  "Denver");
		CreateCity(35.46f, 97.51f,  "Oklahoma City");
		CreateCity(36.16f, 86.78f,  "Nashville");
		CreateCity(31.76f, 106.48f,  "El Paso");
		CreateCity(38.90f, 77.03f,  "Washington DC");
		CreateCity(36.17f, 115.13f,  "Las Vegas");
		CreateCity(42.36f, 71.05f,  "Boston");
	}

	private void _NACities() {
		_USCities();
		_CANCities();
	}
	
	void InitCities()
	{
		// N and W are +ve
		relays = new List<GameObject>();
		
		switch (attack_choice)
		{
			case AttackChoice.Demo:
			case AttackChoice.CoastalUS:
			case AttackChoice.TranscontinentalUS:
			case AttackChoice.Polar:
				_NACities();
				break;
			case AttackChoice.Equatorial:
				// TODO: add cities. (US + SA)
				_USCities();
				break;
			case AttackChoice.IntraOrbital:
				// TODO: add cities. (find a path too)
			case AttackChoice.TransOrbital:
				// TODO: add cities. (find a path too)
				break;
		}
	}

	void CreateCity(float latitude, float longitude, string city_name)
	{
		GameObject city = 
			 (GameObject)Instantiate(city_prefab, new Vector3(0f, 0f, /*-6382.2f*/-6371.0f), transform.rotation);
		
		float long_offset = 20f;
		city.transform.RotateAround(Vector3.zero, Vector3.up, longitude - long_offset);
		Vector3 lat_axis = Quaternion.Euler(0f, -90f, 0f) * city.transform.position;
		city.transform.RotateAround(Vector3.zero, lat_axis, latitude);
		city.transform.SetParent(transform, false);
		
		// Assign a script to the city.
		CityScript cs = (CityScript)city.GetComponent(typeof(CityScript));
		cs.longitude = longitude;
		cs.latitude = latitude;
		cs.city_name = city_name; // TODO: make the target a different color, or not even a CityScript. (It's not correct!)

		// Add the city as a groundstation.
		groundstations.addGroundstation(city, city_name);
	}
	
	// Default way to create a constellation
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

	/* Alternative way to create a constellation (used for August 2019 constellation, as its high inter-plane 
	 * phase offset and high numnber of orbits cause wrapping with CreateSats) */
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
	

	/// <summary>
	/// Utility function to acquire a relay object from a 
	/// </summary>
	/// <param name="nodeid"></param>
	/// <returns></returns>
	GameObject get_relay(int nodeid)
	{
		int relaynum = -(nodeid + 1000);
		return relays[relaynum];
	}

	void highlight_reachable() // CLEANUP: Remove the highlight_reachable() function.
	{
		for (int satnum = 0; satnum < maxsats; satnum++)
		{
			satlist[satnum].BeamOff();
		};
		Node[] reachable = rg.GetReachableNodes();
		print("reachable: " + reachable.Length.ToString());
		for (int i = 0; i < reachable.Length; i++)
		{
			Node rn = reachable[i];
			if (rn.Id >= 0)
			{
				Satellite sat = satlist[rn.Id];
				sat.BeamOn();
			}
		}
	}

	/* Draw the computed path and send traffic in mbits. */
	void ExecuteAttackRoute(Path path, GameObject city1, GameObject city2, int mbits)
	{
		Node rn = path.nodes.First();
		// REVIEW: Separate the drawing functionality from the link capacity modification.
		Debug.Assert(rn != null, "ExecuteAttackRoute | The last node is empty.");
		Debug.Assert(rn.Id == -2, "ExecuteAttackRoute | The last node is not -2. Instead, it's " + rn.Id);
		Node prevnode = null;
		Satellite sat = null;
		Satellite prevsat = null;
		int previd = -4;
		int id = -4; /* first id */
		double prevdist = km_per_unit * rn.Dist;
		int hop = 0;
		int index = 1;
		while (true)
		{

			previd = id;
			id = rn.Id;
			Debug.Log("ID: " + id);

			if (previd != -4)
			{
				// if it's an ISL
				if (previd >= 0 && id >= 0)
				{
					sat = satlist[id];
					prevsat = satlist[previd];

					// Increase the load of the link
					_link_capacities.DecreaseLinkCapacity(previd, id, mbits);
					if (_link_capacities.IsFlooded(previd, id))
					{
						break;
					}

					_painter.ColorRouteISLLink(prevsat, sat, prevnode, rn);
				}
				// if it's an RF
				else
				{
					// TODO: make the link load capacity rule also hold for RF links. 
					if (id >= 0) // the current node is a satellite
					{
						sat = satlist[id];
						// Assert.AreEqual(-2, previd);
						if (previd == -2) // the previous node was a city. Add an active RF link. I guess just add it because it's already been done anyways. in the previous round I mean
						{
							_painter.ColorRFLink(city2, sat, prevnode, rn);
						}
						else // previous ID was -1
						{
							// NB. Not sure what this is either.
							// GameObject city = get_relay(previd);
							// NOTE: I don't think I am ever going to use this! I don't use relays at all!
							// NOTE: TEMPORARY FIX. I NEED TO CHANGE THIS BACK!
							_painter.ColorRFLink(get_relay(previd), sat, prevnode, rn);
						}
					}
					else // the node is a city
					{
						sat = satlist[previd];
						if (id == -1) // NOTE: do we ever enter this edge case?
						{
							_painter.UsedRFLinks.Add(new ActiveRF(city1, rn, sat, prevnode));
						}
						else // if the id is -2
						{
							// Same. I don't think we every enter this edge case.
							GameObject city = get_relay(id);
							_painter.ColorRFLink(city, sat, prevnode, rn);
							_painter.ChangeCityMaterial(city);
						}
					}
				}
			}

			if (rn != null && rn.Id == -1)
			{
				break;
			}
			prevnode = rn;
			// rn = rn.Parent;
			if (index == path.nodes.Count)
			{
				highlight_reachable();
				return;
			}
			rn = path.nodes[index];
			if (rn == null)
			{
				highlight_reachable();
				return;
			}
			index++;
		}

	}
	
	// FIXME: Packets should be sent *from* the source groundstation! Otherwise it's semantically incorrect.
	// slow down when we're close to minimum distance to improve accuracy

	BinaryHeap<Path> FindAttackRoutes(RouteGraph rg, List<GameObject> dest_groundstations) // TODO: Move this function to the attacker object.
	{
		BinaryHeap<Path> heap = new BinaryHeap<Path>(dest_groundstations.Count * _attacker.SourceGroundstations.Count); // priority queue <routegraph, routelength>
		foreach (GameObject src_gs in _attacker.SourceGroundstations)
		{
			foreach (GameObject dest_gs in dest_groundstations)
			{
				if (dest_gs == src_gs)
				{
					continue;
				}

				routeHandler.ResetRoute(src_gs, dest_gs, _painter, satlist, maxsats);
				rg = routeHandler.BuildRouteGraph(src_gs, dest_gs, maxdist, margin, maxsats, satlist, km_per_unit, graph_on, grid);
				rg.ComputeRoutes();
				Debug.Log(groundstations[src_gs] + " to " + groundstations[dest_gs]);

				Path path = _attacker.FindAttackRoute(rg.startnode, rg.endnode, src_gs, dest_gs);
				if (path != null)
				{
					heap.Add(path, (double)path.nodes.Count);
				}
			}
		}
		return heap;
	}

	// Only uncomment for debugging if I need to see the attack sphere.
	void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(_attacker.TargetAreaCenterpoint, _attacker.Radius);
	}

	void 
		RotateCamera()
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
		Debug.Log("Update()");
		
		// Update scene initial parameters
		elapsed_time = last_elapsed_time + (Time.time - last_speed_change) * speed;
		RotateCamera();
		_link_capacities.Reset();
		
		// Clean and rebuild the routegraph.
		RouteHandler.ClearRoutes(_painter);
		// TODO: Create a RouteGraph without needing to have new_york and toronto passed.
		routeHandler.ResetRoute(groundstations["New York"], groundstations["Toronto"], _painter, satlist, maxsats);
		rg = routeHandler.BuildRouteGraph(groundstations["New York"], groundstations["Toronto"], maxdist, margin, maxsats, satlist, km_per_unit, graph_on, grid);
		
		// TODO: Do I need to return the routegraph?
		
		// If the current link isn't valid, select a new target link.
		if (!_attacker.HasValidTargetLink())
		{
			_attacker.ChangeTargetLink(rg, debug_on:true);
			if (_attacker.Link != null)
			{
				Debug.Log("Attacker.Update | Changed link: " + _attacker.Link.SrcNode.Id + " - " + _attacker.Link.DestNode.Id);
			}
		}
		else
		{
			Debug.Log("Attacker.Update | Previous link is still valid.");
		}
		
		// If the attacker has selected a valid link, attempt to attack it
		if (_attacker.Link != null && _attacker.HasValidTargetLink())
		{
			// Find viable attack routes.
			// TODO: Provide all groundstations as input instead of the current limited list.
			BinaryHeap<Path> attack_routes = FindAttackRoutes(rg, new List<GameObject>() { groundstations["Toronto"], groundstations["New York"], groundstations["Chicago"], groundstations["Denver"]});
		
			Debug.Log("Update | There are " + attack_routes.Count + " paths.");
		
			// Create routes until the link's capacity has been reached.
			while (!_link_capacities.IsFlooded(_attacker.Link.SrcNode.Id, _attacker.Link.DestNode.Id) && attack_routes.Count > 0)
			{
				Path attack_path = attack_routes.ExtractMin();
				int mbits = 600;
				if (!RouteHandler.RouteHasEarlyCollisions(attack_path, mbits, _attacker.Link.SrcNode, _attacker.Link.DestNode, _link_capacities))
				{
					Debug.Log("Update | Executing the following path: " + String.Join(" ", from node in attack_path.nodes select node.Id));
					ExecuteAttackRoute(attack_path, attack_path.start_city, attack_path.end_city, mbits);
				}
				else
				{
					Debug.Log("Update | Not executing this path, because it has has early collisions.");
				}
			}
		
			// Debugging messages.
			if (_link_capacities.IsFlooded(_attacker.Link.SrcNode.Id, _attacker.Link.DestNode.Id))
			{
				Debug.Log("Update | Link was flooded.");
			}
			else
			{
				Debug.Log("Update | Link could not be flooded. It has capacity: " + _link_capacities.GetCapacity(_attacker.Link.SrcNode.Id, _attacker.Link.DestNode.Id));
			}
		
			// Color the target link on the map. If flooded, the link is colored red. Otherwise, the link is colored pink.
			_painter.ColorTargetISLLink(satlist[_attacker.Link.SrcNode.Id], satlist[_attacker.Link.DestNode.Id], _attacker.Link.DestNode, _attacker.Link.SrcNode, _link_capacities.IsFlooded(_attacker.Link.SrcNode.Id, _attacker.Link.DestNode.Id));
		}
		else
		{
			Debug.Log("Attacker.Update | Could not find any valid link.");
		}
		
		_painter.UpdateLasers(satlist, maxsats, speed);
		framecount++;
	}
}