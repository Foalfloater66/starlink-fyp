using System;
using System.Collections.Generic;
using System.Linq;
using Attack;
using Orbits.Satellites;
using Routing;
using UnityEngine;
using Scene;
using Scene.GameObjectScripts;
using UnityEngine.UI;
using Utilities;
using Camera = Utilities.Camera;

// TODO: remove the below as well.
public enum RouteChoice { TransAt, TransPac, LonJob, USsparse, USsparseAttacked, USdense, TorMia, Sydney_SFO, Sydney_Tokyo, Sydney_Lima, Followsat };
public enum LogChoice { None, RTT, Distance, HopDists, LaserDists, Path };

public class Main : MonoBehaviour
{
	public Camera cam;
	[Tooltip("Spin: Yes or No")]
	public bool spin;
	[Tooltip("Speed 1 is realtime")]
	public float speed = 1f; // a value of 1 is realtime
	float orig_speed = 1f;
	float simspeed; // actual speed scaled appropriately

	[Tooltip("Enable use of inter-sat lasers")]
	public bool use_isls = true;
	[Tooltip("Enable use of ground relays")]
	public bool use_relays = false;
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

	GameObject london, new_york, san_francisco, singapore, johannesburg, athens, auckland, sydney;
	GameObject northbend, conrad, merrillan, greenville, redmond, hawthorne, bismarck, toronto,
		thunderbay, columbus, lisbon, miami, majorca, tokyo, chicago, lima;


	// Top 25 cities



	private Dictionary<GameObject, string> groundstations = new Dictionary<GameObject, string>();

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

	public RouteChoice route_choice;
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
		Camera camscript = (Camera)cam.GetComponent(typeof(Camera));
		camscript.route_choice = route_choice;
		camscript.InitView();

		int satsperorbit = 0;
		float sat0alt = 0;  // altitiude of sat 0 (other shells may have different altitudes)
		int beam_angle = 0;  // angle above horizon sat is reachable (likely 25 or 40 degrees)
		int orbital_period = 0;
		float phase_offset = 0f;

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

		if (route_choice == RouteChoice.Followsat)
		{
			// We're going to follow a satellite.
			// Need to glue the camera to the satellite.
			followsat_id = 0;
			switch (constellation)
			{
				case ConstellationChoice.P24_S66_A550:
					followsat_id = 78;
					break;
				case ConstellationChoice.P72_S22_A550:
					followsat_id = 1505;
					break;
				case ConstellationChoice.P32_S50_A1100:
					followsat_id = 105;
					break;
			}
			Satellite followsat = satlist[followsat_id];
			cam.transform.position = new Vector3(100, 0, -60);
			cam.transform.rotation = Quaternion.Euler(0, 300, -90);
			cam.transform.SetParent(followsat.gameobject.transform, false);
			followsat.ChangeMaterial(laserMaterials[0]);
			nearest_sats = new Satellite[4];
		}

		//float earthradius = Vector3.Distance (transform.position, london.gameObject.transform.position);
		//km_per_unit2 = (12756f / 2) / earthradius;
		//Debug.Log("km_per_unit: " + km_per_unit.ToString() + " km_per_unit2: " + km_per_unit2.ToString())

		List<GameObject> demo_dest_groundstations = new List<GameObject>() { miami, new_york, chicago };

		// TODO: Add get target cities too. 
		AttackCases.getTargetCoordinates(attack_choice, out float target_lat, out float target_lon);
		_attacker = new Attacker(target_lat, target_lon, sat0r, attack_radius, demo_dest_groundstations, transform, city_prefab);
		_link_capacities = new LinkCapacityMonitor(initial_link_capacity);
	}



	void InitCities()
	{
		// N and W are +ve
		relays = new List<GameObject>();
		london = CreateCity(51.5f, 0f, false);
		new_york = CreateCity(40.76f, 73.98f, false);
		san_francisco = CreateCity(37.733795f, 122.446747f, false);
		singapore = CreateCity(1.290270f, -103.851959f, false);
		johannesburg = CreateCity(-26.1633f, -28.0328f, false);
		athens = CreateCity(37.983810f, -23.727539f, false);
		auckland = CreateCity(-36.84846f, -174.763336f, false);
		sydney = CreateCity(-33.865143f, -151.209900f, false);
		redmond = CreateCity(47.69408f, 122.03222f, false);
		miami = CreateCity(25.768125f, 80.197006f, false);
		tokyo = CreateCity(35.652832f, -139.839478f, false);
		chicago = CreateCity(41.881832f, 87.623177f, false);
		toronto = CreateCity(43.70011f, 79.4163f, false);
		Debug.Log("Created a couple of cities!");
		groundstations = new Dictionary<GameObject, string>(){
			{london, "London"},
			{new_york, "New York"},
			{san_francisco, "San Francisco"},
			{singapore, "Singapore"},
			{johannesburg, "Johannesburg"},
			{athens, "Athens"},
			{auckland, "Auckland"},
			{sydney, "Sydney"},
			{redmond, "Redmond"},
			{miami, "Miami"},
			{tokyo,  "Tokyo"},
			{chicago, "Chicago"},
			{toronto, "Toronto"}
		};


		/*
		//hawthorne = CreateCity (33.92119f, 118.32608f, false);
		*/

		/* transatlantic relays */
		float longdist = 0f;
		float latdist = 40007f / 360f;
		float areasum = 0f;

		if (use_relays)
		{
			switch (route_choice)
			{ // trans-atlantic
				case RouteChoice.TransAt:
					CreateCity(53f, 24f, true);
					CreateCity(51f, 41f, true);

					//CreateCity (42f, 39f, true);

					/* Ireland, west coast */
					CreateCity(52f, 10f, true);
					CreateCity(53f, 10f, true);

					/* Nova Scotia */
					CreateCity(46f, 60f, true);
					CreateCity(44f, 65f, true);

					/* Maine */
					CreateCity(44f, 70f, true);

					/* Newfoundland */
					CreateCity(48f, 58f, true);
					CreateCity(49f, 54f, true);
					CreateCity(51.374132f, 55.581248f, true); // St Anthony

					/* Quebec City */
					CreateCity(46.804304f, 71.212131f, true);
					CreateCity(46.804304f, 72.212131f, true);

					CreateCity(50.132574f, 61.801297f, true); // Pointe-Parent, Quebec

					/* Cloridorme, New Brunswick */
					CreateCity(49.178832f, 64.836269f, true);


					/* Azores */
					CreateCity(39.449375f, 31.209317f, true);
					CreateCity(37.831072f, 25.141654f, true);

					/* Madeira */
					CreateCity(32.857646f, 17.198472f, true);

					/* Finistere, Spain */
					CreateCity(42.923941f, 9.279052f, true);

					/* Marrakesh*/
					CreateCity(31.655479f, 7.977596f, true);

					/* Laayoune, Western Sahara */
					CreateCity(27.156665f, 13.233910f, true);

					/* Las Palmas, Canary Islands */
					CreateCity(28.100000f, 15.451542f, true);

					/* Lisbon */
					lisbon = CreateCity(38.709687f, 9.485385f, true);
					break;


				case RouteChoice.LonJob:
					//relay_dist_step = 2.0f;
					/* Madeira */
					CreateCity(32.857646f, 17.198472f, true);

					/* Finistere, Spain */
					CreateCity(42.923941f, 9.279052f, true);

					/* Marrakesh*/
					CreateCity(31.655479f, 7.977596f, true);

					/* Laayoune, Western Sahara */
					CreateCity(27.156665f, 13.233910f, true);

					/* Las Palmas, Canary Islands */
					CreateCity(28.100000f, 15.451542f, true);

					/* Lisbon */
					lisbon = CreateCity(38.709687f, 9.485385f, true);

					// mediterranean
					CreateCity(45.4655f, -9.1865f, true);
					CreateCity(41.9028f, -12.4964f, true);
					CreateCity(40.8518f, -14.2681f, true);
					CreateCity(37.3979f, -14.6588f, true);
					CreateCity(41.0128f, -8.875f, true);
					CreateCity(35.9375f, -14.3754f, true);
					CreateCity(39.6953f, -3.0176f, true);

					// N Africa
					CreateCity(36.8188f, -10.166f, true);  // Tunis
					CreateCity(32.894548f, -13.182726f, true); // Tripoli
					CreateCity(18.063929f, 15.969875f, true); //Nouakchott, Mauritania
					CreateCity(16.953450f, 0.351920f, true); // Bourem, Mali
					CreateCity(13.523209f, -2.120887f, true); // Niamey, Miger
					CreateCity(16.968058f, -7.989515f, true); //Agadaz, Niger
					CreateCity(22.889711f, -4.847936f, true); // Abalessa, Algeria
					CreateCity(27.879648f, 0.287662f, true); // Adrar, Algeria
					CreateCity(26.719830f, 0.170543f, true); // Reggane, Algeria
					CreateCity(26.587697f, -12.775448f, true); // Ubari, Libya
					CreateCity(27.036485f, -14.422205f, true); // Sabha, Libya
					CreateCity(32.092705f, -20.087222f, true); // Benghazi, Libya
					CreateCity(31.204647f, -16.583952f, true); // Sirte, Libya
					CreateCity(29.030742f, -21.549600f, true); // Jalu, Libya
					CreateCity(25.672835f, -21.074561f, true); // Tazirbu, Libya
					CreateCity(14.423622f, -6.044345f, true); // Bouza, Niger
					CreateCity(18.739735f, -7.392920f, true); // Arlit, Niger
					CreateCity(16.771889f, 3.006630f, true); // Timbuktu, Mali
					CreateCity(16.613272f, 7.256843f, true); // Nema, Mauritania
					CreateCity(20.512436f, 13.045014f, true); //Atar, Mauritania
					CreateCity(26.739824f, 11.680986f, true); //Samara, W. Sahara
					CreateCity(14.253680f, -13.114089f, true); //N'Guigmi, Niger
					CreateCity(12.632408f, 8.009227f, true); // Bamaki, Mali
					CreateCity(14.492812f, 4.192475f, true); // Mopti, Mali
					CreateCity(14.947206f, 3.893295f, true); // Kona, Mali
					CreateCity(14.119974f, -15.312130f, true); // Mao, Chad
					CreateCity(13.643129f, -16.492301f, true); // Moussoro, Chad
					CreateCity(14.972823f, -8.880027f, true); // Tanout, Niger
					CreateCity(16.277464f, 0.046189f, true); // Gao, Mali
					CreateCity(15.181080f, -0.720703f, true); // Ouatagouna, Mali
					CreateCity(30.052325f, -31.234923f, true); // Cairo
					CreateCity(29.069891f, -31.095649f, true); // Ben Sweif, Egypt
					CreateCity(24.088748f, -32.896148f, true); // Aswan, Egypt
					CreateCity(22.371821f, -31.610475f, true); // Abu Simbel, Egypt
					CreateCity(15.508254f, -32.519168f, true); // Khartoum, Sudan
					for (float lat = 38f; lat < 44f; lat += relay_dist_step)
					{
						longdist = (40075f / 360f) * Mathf.Cos(Mathf.Deg2Rad * (lat + relay_dist_step / 2f));
						float area = (latdist * relay_dist_step) * (longdist * relay_dist_step);
						for (float lng = 2f; lng < 10f; lng += relay_dist_step)
						{
							CreateCity(lat, lng, true);
							areasum += area;
						}
					}

					for (float lng = -10f; lng < 0f; lng += relay_dist_step)
					{
						CreateCity(36f, lng, true);
					}
					for (float lat = 32f; lat < 36f; lat += relay_dist_step)
					{
						longdist = (40075f / 360f) * Mathf.Cos(Mathf.Deg2Rad * (lat + relay_dist_step / 2f));
						float area = (latdist * relay_dist_step) * (longdist * relay_dist_step);
						for (float lng = -10f; lng < 8f; lng += relay_dist_step)
						{
							CreateCity(lat, lng, true);
							areasum += area;
						}
					}
					for (float lat = 6f; lat < 14f; lat += relay_dist_step)
					{
						longdist = (40075f / 360f) * Mathf.Cos(Mathf.Deg2Rad * (lat + relay_dist_step / 2f));
						float area = (latdist * relay_dist_step) * (longdist * relay_dist_step);
						for (float lng = -40f; lng < 14f; lng += relay_dist_step)
						{
							CreateCity(lat, lng, true);
							areasum += area;
						}
					}
					for (float lat = 0f; lat < 6f; lat += relay_dist_step)
					{
						longdist = (40075f / 360f) * Mathf.Cos(Mathf.Deg2Rad * (lat + relay_dist_step / 2f));
						float area = (latdist * relay_dist_step) * (longdist * relay_dist_step);
						for (float lng = -40f; lng < -14f; lng += relay_dist_step)
						{
							CreateCity(lat, lng, true);
							areasum += area;
						}
					}
					for (float lat = -20f; lat < 0f; lat += relay_dist_step)
					{
						longdist = (40075f / 360f) * Mathf.Cos(Mathf.Deg2Rad * (lat + relay_dist_step / 2f));
						float area = (latdist * relay_dist_step) * (longdist * relay_dist_step);
						for (float lng = -40f; lng < -12f; lng += relay_dist_step)
						{
							CreateCity(lat, lng, true);
							areasum += area;
						}
					}
					break;


				case RouteChoice.TransPac:
					// transpacific relays
					TransPacCities();
					//CreateCity (51f, 145f, true);  // ship, gulf of alaska
					break;
				case RouteChoice.USsparseAttacked:
				case RouteChoice.USsparse:
					northbend = CreateCity(47.48244f, 121.76131f, true);
					conrad = CreateCity(48.2033f, 111.94527f, true);
					bismarck = CreateCity(46.80833f, 100.78374f, true);
					merrillan = CreateCity(43.40633f, 90.81427f, true);
					greenville = CreateCity(41.43355f, 80.33322f, true);
					CreateCity(51.17622f, 115.56982f, true); // Banff
					CreateCity(49.8844f, 97.14704f, true); // Winnipeg
					CreateCity(52.11679f, 106.63452f, true); // Saskatoon
					CreateCity(39.11417f, 94.62746f, true); // Kansas city
					CreateCity(39.73915f, 104.9847f, true); // Denver
					CreateCity(36.253368f, 115.066733f, true); // Vegas
					toronto = CreateCity(43.70011f, 79.4163f, true);
					thunderbay = CreateCity(48.38202f, 89.25018f, true);
					columbus = CreateCity(39.96118f, 82.99879f, true);
					break;

				case RouteChoice.TorMia:
				case RouteChoice.USdense:
					int[] startrows = { 13, 13, 13, 12, 11, 9, 8, 8, 7, 6, 6, 5, 3, 3, 2 };
					int[] endrows = { 45, 45, 49, 50, 50, 53, 54, 55, 55, 56, 56, 57, 57, 57, 57, 57, 57, 57, 57 };
					int[] holestart = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 13, 19, 19, 14, 0, 18, 19, 0, 0, 10, 0, 0, 0 };
					int[] holestop = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15, 20, 20, 16, 0, 20, 22, 0, 0, 26, 30, 31, 35 };

					//relay_dist_step = 1.0f;
					float latcount = 0;
					for (float lat = 30f; lat < 55f; lat += relay_dist_step)
					{
						longdist = (40075f / 360f) * Mathf.Cos(Mathf.Deg2Rad * (lat + relay_dist_step / 2f));
						float area = (latdist * relay_dist_step) * (longdist * relay_dist_step);
						float lngcount = 0;
						for (float lng = 68f; lng < 126f; lng += relay_dist_step)
						{
							if (latcount >= startrows.Length || lngcount > startrows[(int)latcount])
							{
								if (latcount >= endrows.Length || lngcount < endrows[(int)latcount])
								{
									if (lngcount >= holestop[(int)latcount] || lngcount < holestart[(int)latcount])
									{
										CreateCity(lat, lng, true);
										areasum += area;
									}
								}
							}
							lngcount += relay_dist_step;
						}
						latcount += relay_dist_step;
					}
					break;
				case RouteChoice.Sydney_SFO:
					TransPacCities();
					WPacificCities();
					AusCities();
					break;

				case RouteChoice.Sydney_Tokyo:
					TransPacCities();
					WPacificCities();
					AusCities();
					break;

				case RouteChoice.Sydney_Lima:
					SPacificCities();
					AusCities();

					break;
				case RouteChoice.Followsat:
					// we just follow a satellite - no need for cities
					break;
			}

		}
	}

	void TransPacCities()
	{
		float longdist = 0f;
		float latdist = 40007f / 360f;
		float areasum = 0f;
		// transpacific relays
		CreateCity(52.834065f, -173.171531f, true);  //Attu Station, AK, USA
		CreateCity(51.867554f, 176.638798f, true);  //Adak, Alaska, USA
		CreateCity(52.212841f, 174.207698f, true);  //Atka, AK 99547, USA
		CreateCity(52.939563f, 168.860887f, true);  //Nikolski, AK 99638, USA
		CreateCity(53.889189f, 166.538114f, true);  //Amaknak Island, Unalaska, AK, USA
		CreateCity(54.133662f, 165.776601f, true);  //Akutan, AK 99553, USA
		CreateCity(55.059223f, 162.312326f, true);  //King Cove, AK, USA
		CreateCity(56.944874f, 154.168453f, true);  //Akhiok, AK 99615, USA
		CreateCity(57.204254f, 153.304328f, true);  //Old Harbor, AK, USA
		CreateCity(57.754380f, 152.492349f, true);  //Kodiak Station, AK 99615, USA
		CreateCity(59.349383f, 151.829452f, true);  //Homer, AK 99603, USA
		CreateCity(59.797965f, 144.598995f, true);  //Kayak Island State Marine Park, Alaska, USA
		CreateCity(59.509158f, 139.670835f, true);  //Yakutat, AK 99689, USA
		CreateCity(57.960182f, 136.230894f, true);  //Pelican, AK 99832, USA
		CreateCity(56.247291f, 134.647728f, true);  //Port Alexander, AK 99836, USA
		CreateCity(54.310081f, 130.323739f, true);  //Prince Rupert, BC V8J 3K8, Canada
		CreateCity(43.384242f, -145.810615f, true);  //Nosappu, Nemuro, Hokkaido 087-0165, Japan
		CreateCity(43.869647f, -146.828961f, true);  //Unnamed Road, Shikotan-mura 694520
		CreateCity(50.676459f, -156.139535f, true);  //Severo-Kurilsky District, Sakhalin Oblast, Russ
		CreateCity(39.641144f, -141.952581f, true);  //Ōdōri, Miyako-shi, Iwate-ken 027-0083, Japan
		CreateCity(42.011085f, -143.150735f, true);  //Erimo-chō, Horoizumi-gun, Hokkaidō 058-0203, Japan
		CreateCity(53.253489f, 132.117766f, true);  //Queen Charlotte F, BC, Canada
		CreateCity(50.723849f, 127.496263f, true);  //Port Hardy, BC V0N 2P0, Canada
		//relay_dist_step = 2.0f;
		for (float lat = 30f; lat < 55f; lat += relay_dist_step)
		{
			longdist = (40075f / 360f) * Mathf.Cos(Mathf.Deg2Rad * (lat + relay_dist_step / 2f));
			float area = (latdist * relay_dist_step) * (longdist * relay_dist_step);
			for (float lng = 72f; lng < 122f; lng += relay_dist_step)
			{
				CreateCity(lat, lng, true);
				areasum += area;
			}
		}
	}

	void WPacificCities()
	{
		CreateCity(-9.459888f, -147.187468f, true);  // Port Morsby, PNG
		CreateCity(-0.875620f, -131.246103f, true);  // Sorong, West Papua
		CreateCity(-8.511734f, -126.015478f, true); // Manatuto, Timor-Leste
		CreateCity(-2.605694f, -140.677133f, true);  // Jayapura, Indonesia
		CreateCity(0.787670f, -127.386715f, true);  // Ternate City, Indonesia
		CreateCity(7.079770f, -125.618577f, true);  // Davao, Phillipines
		CreateCity(14.587267f, -120.992825f, true);  // Manilla, Phillipines
		CreateCity(18.582077f, -120.785173f, true);  // Burayoc, Phillipines
		CreateCity(22.002509f, -120.743952f, true);  // Hengchung, Taiwan
		CreateCity(25.153802f, -121.747830f, true);  // Keelung, Taiwan
		CreateCity(30.370269f, -130.882104f, true);  // Nishino, Kagoshima, Japan
		CreateCity(31.563340f, -130.553916f, true);  // Kagoshima City, Japan
	}

	void AusCities()
	{
		CreateCity(-12.463968f, -130.842777f, true);  // Darwin, AUS
		CreateCity(-16.920180f, -145.769174f, true);  // Cairns, AUS
		CreateCity(-27.501833f, -153.060355f, true);  // Brisbane, AUS
		CreateCity(-12.185451f, -136.788007f, true);  // Nuhlunbuy, AUS
		CreateCity(-17.671119f, -141.078417f, true);  // Normanton, QND, AUS
		CreateCity(-23.704273f, -133.875712f, true);  // Alice Springs, AUS
		CreateCity(-31.525911f, -159.065333f, true);  // Lord Howe Island, AUS
	}

	void SPacificCities()
	{
		CreateCity(-29.031895f, -167.946841f, true);  // Burnt Pine, Norfolk Island
		CreateCity(-22.274674f, -166.452682f, true); // Noumea, New Caledonia
		CreateCity(-18.143231f, -178.435968f, true); // Suva, Fiji
		CreateCity(-13.834423f, 171.760831f, true);  // Apia, Samoa
		CreateCity(-14.220016f, 169.423424f, true);  // Maia, Manu'a, American Samoa
		CreateCity(-21.207587f, 159.783645f, true);  // Cook Islands
		CreateCity(-21.926098f, 157.955635f, true);  // Oneroa, Cook Islands
		CreateCity(-17.558133f, 149.600364f, true);  // F'a'a', French Polynesia
		CreateCity(-14.959108f, 147.646249f, true);  // Rangiroa, French Polynesia
		CreateCity(-16.067234f, 145.614420f, true);  // Rotoava, French Polynesia
		CreateCity(-17.354274f, 138.444553f, true);  // Tatakoto, French Polynesia
		CreateCity(-23.122389f, 134.968636f, true);  // Rikitea, French Polynesia
		CreateCity(-25.066740f, 130.099011f, true);  // Adamstown, Pitcairn Islands
		CreateCity(-24.333670f, 128.303854f, true);  // Henderson Island, Pitcairn Islands (uninhabited)
		CreateCity(-24.674048f, 124.777367f, true);  // Ducie, Pitcairn Islands (uninhabited)
		CreateCity(-25.91f, 117.1f, true);  // ship
		CreateCity(-27.149430f, 109.428944f, true);  // Easter Island
		//CreateCity(-22.14f, 98.75f, true); // ship
		//CreateCity(-17.03f, 87.7f, true); // ship
		//CreateCity(-21.523945f, 92.142192f, true); // ship
		CreateCity(-12.073062f, 77.065722f, true); // Lima, Peru
		CreateCity(-18f, 103f, true); // ship
		CreateCity(-9f, 97f, true); // ship
		CreateCity(-0.956546f, 90.968258f, true); // Puerto Villamil, Galapagos, Ecuador
		lima = CreateCity(-1.069440f, 80.907160f, true); // San Lorenzo, Ecuador
	}

	GameObject CreateCity(float latitude, float longitude, bool is_relay, string name = null /* only to take account of certain cities that don't have names */)
	{
		GameObject city = 
			 (GameObject)Instantiate(city_prefab, new Vector3(0f, 0f, /*-6382.2f*/-6371.0f), transform.rotation);
		
		float long_offset = 20f;
		city.transform.RotateAround(Vector3.zero, Vector3.up, longitude - long_offset);
		Vector3 lat_axis = Quaternion.Euler(0f, -90f, 0f) * city.transform.position;
		city.transform.RotateAround(Vector3.zero, lat_axis, latitude);
		city.transform.SetParent(transform, false);
		CityScript cs = (CityScript)city.GetComponent(typeof(CityScript));
		cs.longitude = longitude;
		cs.latitude = latitude;
		cs.name = name;
		if (is_relay)
		{
			grid.AddCity(latitude, longitude, relays.Count, city);
			relays.Add(city);
		}
		return city;
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
		// FIXME: Separate the drawing functionality from the link capacity modification.
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
						if (previd == -2) // the previous node was a city. Add an active RF link. I guess just add it because it's already been done anyways. in the previous round I mean
						{
							_painter.UsedRFLinks.Add(new ActiveRF(city2, prevnode, sat, rn));
						}
						else // previous ID was -1
						{
							// NB. Not sure what this is either.
							// GameObject city = get_relay(previd);
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
		Debug.Log("Update()");
		
		// Update scene initial parameters
		elapsed_time = last_elapsed_time + (Time.time - last_speed_change) * speed;
		RotateCamera();
		_link_capacities.Reset();
		
		// Clean and rebuild the routegraph.
		RouteHandler.ClearRoutes(_painter);
		routeHandler.ResetRoute(new_york, toronto, _painter, satlist, maxsats);
		rg = routeHandler.BuildRouteGraph(new_york, toronto, maxdist, margin, maxsats, satlist, km_per_unit, graph_on, grid);
		
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
		if (_attacker.HasValidTargetLink())
		{
			// Find viable attack routes.
			// TODO: Provide all groundstations as input instead of the current limited list.
			BinaryHeap<Path> attack_routes = FindAttackRoutes(rg, new List<GameObject>() { toronto, new_york, chicago });
		
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