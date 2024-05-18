﻿using System.Collections.Generic;
using System.IO;
using Attack;
using Orbits.Satellites;
using Routing;
using Scene;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Serialization;
using Utilities;

namespace Orbits
{
    public class Constellation 
    {
    public float speed = 1f; // a value of 1 is realtime
    //
    // TODO: WILL THIS APPEAR IN THE INSPECTOR? :0
    public float simspeed; // actual speed scaled appropriately
    //
    // // [Tooltip("Enable use of inter-sat lasers")]
    public bool use_isls = true;
    //
    // // [HideInInspector] public float direction = 1f;
    // // [HideInInspector] public float directionChangeSpeed = 2f;
    // // [HideInInspector] public bool graph_on;
    //
    //
    public GameObject orbit;
    public GameObject satellite;
    public GameObject laser;
    public GameObject thin_laser;
    // // public GameObject city_prefab;
    // // public Material isl_material;
    // // public Material[] laserMaterials;
    // // public Material[] targetLinkMaterial; // 1st is link up, 2nd is link down.
    // // public Material cityMaterial;
    // // [Space] private Captures captures;
    //
    // // private const double earthperiod = 86400f;
    // private Attacker _attacker;
    //
    //
    public GameObject beam_prefab;
    public GameObject beam_prefab2;
    //
    private Dictionary<int, List<int>>
        orbit2sats = new Dictionary<int, List<int>>(); /* Orbit ID mapping to satellite ID list. */
    //
    // private LinkCapacityMonitor _link_capacities; /* (node1, node2) link mapping to capacity. Links are full duplex. */
    //
    // private CityCreator _city_creator;
    public GameObject[] orbits;
    public double[] orbitalperiod;
    public Satellite[] satlist;
    public Vector3[] orbitaxes;
    public int orbitcount = 0, satcount = 0;
    //
    // private float elapsed_time;
    // private float last_speed_change;
    // private float last_elapsed_time;
    // private int framecount = 0;
    public int maxsats; // total number of satellites
    private int phase1_sats; // number of satellites in phase 1
    public int maxorbits;
    private const int maxlasers = 4;
    private int isl_plane_shift = 0;
    private int isl_plane_step = 0;
    private bool isl_connect_plane = true;
    private int isl_plane2_shift = 0;
    private int isl_plane2_step = 0;
    // private float pause_start_time;
    // private Node[] nodes;
    // private ScenePainter _painter;
    public float km_per_unit;
    // private ConstellationContext constellation_ctx;
    // private StreamWriter _fileWriter;
    // private StreamWriter _pathLogger;
    // private readonly GroundstationCollection groundstations = new GroundstationCollection();
    // private RouteHandler _routeHandler;
    // private GameObject[] lasers;
    // private int lastpath; // used for multipaths
    // private Satellite[] nearest_sats; // used for calculating collision distances
    private StreamWriter logfile;  // TODO: I want to remove this.
    public float maxdist = 0f;
    private float beam_radius = 0f;
    public float margin = 100f;
    public float sat0r;
    private float raan0 = 0f;
    private int decimator = 1; // was originally modifiable.

    // private Transform _mainTransform;
    //
    private Main.BeamChoice beam_on;
    private LogChoice log_choice;
    
    public Constellation(Transform transform, GameObject orbit, GameObject satellite, GameObject beam_prefab, GameObject beam_prefab2, GameObject laser, GameObject thinLaser, Main.BeamChoice beam_on, LogChoice log_choice, float speed, float simspeed, bool use_isls, StreamWriter logfile)
    {
        // Initialize GameObjects
        this.orbit = orbit;
        this.satellite = satellite;
        this.beam_prefab = beam_prefab;
        this.beam_prefab2 = beam_prefab2;
        this.laser = laser;
        this.thin_laser = thinLaser;
        
        this.beam_on = beam_on;
        this.log_choice = log_choice;
        this.speed = speed;
        this.simspeed = simspeed;
        this.use_isls = use_isls;
        // Init(transform);
        // Build();

        this.logfile = logfile;
        
        Init(transform);
        Build();


    }

        public void Init(Transform transform)
        {
                orbitcount = 0;
                satcount = 0;
                maxorbits = 24 / decimator;
                var satsperorbit = 66; // shell altitude
                var sat0alt = 550f;
                var beam_angle = 25; // angle above horizon sat is reachable (likely 25 or 40 degrees)
                maxdist = 1123f; // max RF distance from sat to ground station
                beam_radius = 940f;
                var orbital_period = 5739; // seconds
                isl_connect_plane = true;
                isl_plane_shift = -1; // isl offset to next plane
                isl_plane_step = 1;
                var phase_offset = 13f / 24f;
                maxsats = maxorbits * satsperorbit;
                phase1_sats = maxsats; // will differ if simulating multiple phases
                simspeed = speed * 360f / 86400f; // Speed 1 is realtime

                orbits = new GameObject[maxorbits];
                orbitalperiod = new double[maxorbits];
                orbitaxes = new Vector3[maxorbits];
        
                /* new model */
                satlist = new Satellite[maxsats];
                const float earth_r = 6371f; // earth radius
                sat0r = sat0alt + earth_r; // sat radius from earth centre
                // beam_angle, beam_radius);
                CreateSats(maxorbits, satsperorbit, 53f, 0f, 0f, phase_offset, orbital_period, sat0r,
                    beam_angle, beam_radius, transform);
                var earthdist = Vector3.Distance(satlist[0].gameobject.transform.position, transform.position);
                km_per_unit = sat0r / earthdist; // sim scale factor

        }

        public void Build()
        {
                // Create satellites
                for (var satnum = 0; satnum < maxsats; satnum++)
                {
                    satlist[satnum].glow = false;
                    for (var s2 = 0; s2 < maxsats; s2++)
                        if (satnum != s2)
                            satlist[satnum].AddSat(satlist[s2]);
                }

                // Draw existing links between satellites.
                for (var satnum = 0; satnum < maxsats; satnum++)
                    if (use_isls)
                    {
                        if (isl_connect_plane)
                            // connect lasers along orbital plane
                            satlist[satnum].PreAssignLasersOrbitalPlane();
                        else
                            satlist[satnum].PreAssignLasersBetweenPlanes(isl_plane2_shift, isl_plane2_step);
                        satlist[satnum].PreAssignLasersBetweenPlanes(isl_plane_shift, isl_plane_step);
            }
        }

        // public ConstellationContext GetContext()
        // {
        //     // Constellation context.
        //     constellation_ctx = new ConstellationContext
        //     {
        //         satlist = satlist,
        //         km_per_unit = km_per_unit,
        //         maxsats = maxsats,
        //         maxdist = maxdist,
        //         margin = margin
        //     };
        //     return constellation_ctx;
        // }
        /// <summary>
    /// Default way to create a constellation
    /// </summary>
    private void CreateSats(int num_orbits, int sats_per_orbit, float inclination, float orbit_phase_offset,
        float sat_phase_offset, float sat_phase_stagger, double period, float altitude,
        int beam_angle, float beam_radius, Transform transform)
    {
        var orbit_angle_step = 360f / num_orbits;
        for (var i = 0; i < num_orbits; i++)
        {
            orbits[orbitcount] = (GameObject)Main.Instantiate(orbit, transform.position, transform.rotation);
            orbits[orbitcount].transform.RotateAround(Vector3.zero, Vector3.forward, inclination);
            var orbit_angle = orbit_phase_offset * orbit_angle_step + i * orbit_angle_step + raan0;
            orbits[orbitcount].transform.RotateAround(Vector3.zero, Vector3.up, orbit_angle);
            orbitaxes[orbitcount] =
                Quaternion.Euler(0, orbit_angle, 0) * (Quaternion.Euler(0, 0, inclination) * Vector3.up);
            orbitalperiod[orbitcount] = period;
            orbits[orbitcount].transform.localScale = new Vector3(altitude, altitude, altitude);
            for (var s = 0; s < sats_per_orbit; s++)
            {
                double sat_angle_step = 360f / sats_per_orbit;
                var sat_angle = -1f * sat_phase_offset * sat_angle_step + i * sat_angle_step * sat_phase_stagger +
                                s * sat_angle_step;
                var newsat = new Satellite(satcount, s, i, transform, orbits[orbitcount],
                    sat_angle, maxlasers, maxsats, phase1_sats, sat_phase_stagger, sats_per_orbit, num_orbits,
                    altitude, beam_angle, beam_radius, satellite, beam_prefab, beam_prefab2, laser, thin_laser,
                    logfile, log_choice);
                satlist[satcount] = newsat;

                // Add the satellite to the orbit's list of satellites.
                if (!orbit2sats.ContainsKey(i)) orbit2sats.Add(i, new List<int>());
                orbit2sats[i].Add(newsat.satid);

                if (beam_on == Main.BeamChoice.AllOn) newsat.BeamOn();
                satcount++;
            }

            var os = (OrbitScript)orbits[orbitcount].GetComponent(typeof(OrbitScript));
            os.orbit_id = i;

            orbitcount++;
        }
    }

    }
}