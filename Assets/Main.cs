using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

public enum LogChoice
{
    None,
    RTT,
    Distance,
    HopDists,
    LaserDists,
    Path
};

public class Main : MonoBehaviour
{
    [Header("Environment")] public CustomCamera cam;

    [Tooltip("Speed 1 is realtime")] public float speed = 1f; // a value of 1 is realtime

    private float simspeed; // actual speed scaled appropriately

    [Tooltip("Enable use of inter-sat lasers")]
    public bool use_isls = true;

    [HideInInspector] public float direction = 1f;
    [HideInInspector] public float directionChangeSpeed = 2f;
    [HideInInspector] public bool graph_on;


    [Header("Objects & Materials")] public GameObject orbit;
    // GameObjects
    public GameObject satellite;
    public GameObject laser;
    [FormerlySerializedAs("thin_laser")] public GameObject thinLaser;
    [FormerlySerializedAs("city_prefab")] public GameObject cityPrefab;
    [FormerlySerializedAs("isl_material")] public Material islMaterial;
    public Material[] laserMaterials;
    public Material[] targetLinkMaterial; // 1st is link up, 2nd is link down.
    public Material cityMaterial;
    
    // Text
    [FormerlySerializedAs("countdown")] public Text leftbottom;
    public Text rightbottom;
    public Text topleft;
    [Space] private Captures captures;

    private const double earthperiod = 86400f;
    private Attacker _attacker;


    [FormerlySerializedAs("beam_prefab")] [HideInInspector] public GameObject beamPrefab;
    [FormerlySerializedAs("beam_prefab2")] [HideInInspector] public GameObject beamPrefab2;

    // private Dictionary<int, List<int>>
    //     orbit2sats = new Dictionary<int, List<int>>(); /* Orbit ID mapping to satellite ID list. */

    private LinkCapacityMonitor _link_capacities; /* (node1, node2) link mapping to capacity. Links are full duplex. */

    private CityCreator _city_creator;
    // private GameObject[] orbits;
    // private double[] orbitalperiod;
    // private Satellite[] satlist;
    // private Vector3[] orbitaxes;
    // private int orbitcount = 0, satcount = 0;

    private float elapsed_time;
    private float last_speed_change;
    private float last_elapsed_time;
    private int framecount = 0;
    // private const int maxlasers = 4;
    // private int isl_plane_shift = 0;
    // private int isl_plane_step = 0;
    // private bool isl_connect_plane = true;
    // private int isl_plane2_shift = 0;
    // private int isl_plane2_step = 0;
    private float pause_start_time;
    private Node[] nodes;
    private ScenePainter _painter;
    // private float km_per_unit;
    // private ConstellationContext constellation_ctx;
    private StreamWriter _fileWriter;
    private StreamWriter _pathLogger;
    private readonly GroundstationCollection groundstations = new GroundstationCollection();
    private RouteHandler _routeHandler;
    private GameObject[] lasers;
    private int lastpath; // used for multipaths
    private Satellite[] nearest_sats; // used for calculating collision distances
    private StreamWriter logfile;
    private float maxdist = 0f;

    private string _loggingDirectory;

    [Header("Attack Parameters")]
    [FormerlySerializedAs("qualitativeCase")]
    [FormerlySerializedAs("attackArea")]
    [FormerlySerializedAs("attack_choice")]
    public CaseChoice caseChoice;

    public Direction targetLinkDirection;
    public float attack_radius = 1000f;

    [HideInInspector] public bool deterministic_attacker; // YES OR NO...?

    private Constellation _constellation;

    // Deprecated log choice.
    private LogChoice log_choice = LogChoice.None;

    // [HideInInspector]
    public enum BeamChoice
    {
        AllOff,
        AllOn,
        SrcDstOn
    };

    // [HideInInspector]
    public BeamChoice beam_on;

    [Header("Defence Parameters")] public bool defenceOn = false;
    private Router _router;

    [Header("Logging")] public bool captureMode = true;
    
    private CustomCamera InitCamera()
    {
        var camscript = (CustomCamera)cam.GetComponent(typeof(CustomCamera));
        camscript.SetupView();
        return camscript;
    }

    private void InitTime()
    {
        // Handle time in a way that copes with speed change mid-simulation.
        last_elapsed_time = Time.time;
        last_speed_change = last_elapsed_time;
        elapsed_time = 0f;
    }

    private void InitLogging()
    {
        // Create logging directory.
        _loggingDirectory = $"{Directory.GetCurrentDirectory()}/Logs/Captures/{caseChoice}_{targetLinkDirection}";
        if (Directory.Exists(_loggingDirectory)) Directory.Delete(_loggingDirectory, true);
            Directory.CreateDirectory(_loggingDirectory);
        
        // Save a screenshot of each frame.
        if (captureMode)
        {
            captures = new Captures(_loggingDirectory, $"{caseChoice}_{targetLinkDirection}");
        }
    
        // Record information about snapshots.
        _fileWriter = new StreamWriter(Path.Combine(_loggingDirectory, $"{caseChoice}_{targetLinkDirection}.csv"));
        _fileWriter.WriteLine("FRAME,TARGET LINK,ROUTE COUNT,FINAL CAPACITY"); // todo: should I do average latency?
        
        // Record information about the attack routes selected for each frame.
        _pathLogger = new StreamWriter(Path.Combine(_loggingDirectory, "paths.csv"));
        _pathLogger.WriteLine("FRAME,PATHS");
    }

    private void InitScene()
    {
        // GameObjects
        _city_creator = new CityCreator(transform, cityPrefab, groundstations);
        _constellation = new Constellation(transform, orbit, satellite, beamPrefab, beamPrefab2, laser, thinLaser, beam_on, log_choice, speed, simspeed, use_isls, logfile);
        // UI Text + Visuals.
        rightbottom.text = "";
        topleft.text = "";
        _painter = new ScenePainter(islMaterial, laserMaterials, targetLinkMaterial, cityMaterial);
    }

    private void InitRoutingFramework()
    {
        // Routing + Stats Monitoring
        _link_capacities = new LinkCapacityMonitor();
        _routeHandler = new RouteHandler(_painter);
        _routeHandler.InitRoute(_constellation.maxsats, _constellation.satlist, _constellation.maxdist, _constellation.km_per_unit);
        _router = new Router(defenceOn, groundstations, _routeHandler, _painter, _link_capacities, _constellation, _fileWriter, _constellation.km_per_unit);
    }

    private void Start()
    {
        // Simulation configuration.
        InitTime();
        Application.runInBackground = true;
        InitLogging();
        InitScene();
        var camscript = InitCamera();
        var attackerParams = CasesFactory
            .GetCase(caseChoice, _city_creator, targetLinkDirection, groundstations, camscript).GetParams();
        InitRoutingFramework();
        _attacker = new Attacker(attackerParams, _constellation.sat0r, attack_radius, transform, cityPrefab, groundstations,
            _routeHandler, _painter, _link_capacities, _constellation, _fileWriter, _pathLogger);
        camscript.InitView();
        
        // Give the program enough time to generate all game objects when capture mode is disabled.
        Thread.Sleep(10000); 
    }

    // Only uncomment for debugging if I need to see the attack sphere.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_attacker.Target.Center, _attacker.Target.Radius);
    }

    private void RotateCamera()
    {
        // Update the direction of the orbits
        if (direction < 1f) direction += Time.deltaTime / (directionChangeSpeed / 2);

        // Move the orbits
        for (var i = 0; i < _constellation.maxorbits; i++)
            _constellation.orbits[i].transform.RotateAround(Vector3.zero, _constellation.orbitaxes[i],
                (float)(-1f * earthperiod / _constellation.orbitalperiod[i] * _constellation.simspeed * direction) * Time.deltaTime);
    }

    private void LogRTT(List<Routing.Path> routes)
    {
        var rttLog = "Latency: ";
        for (int idx = 0; idx < routes.Count; idx++)
        {
            float rtt = routes[idx].GetRTT(_constellation.km_per_unit);
            if ( rtt > 2000f)
            {
                rttLog += "Fail!";
            }
            else if (idx == 0)
            {
                rttLog += $"{(int)rtt} ms";
            }
            else
            {
                if (idx % 5 == 0)
                    rttLog += ",\n";
                else
                    rttLog += ", ";
                rttLog += $"{(int)rtt} ms";
            }
        }

        topleft.text = rttLog;
    }

    /// <summary>
    /// Update function to run the main routegraph rebuild + attacker + frame recording (if enabled)
    /// </summary>
    private void Update()
    {
        // TODO: DEMO MODE NEEDS TO PAUSE FOR EACH STEP/TAKE A SCREENSHOT.
        // Update / Reset scene parameters.
        elapsed_time = last_elapsed_time + (Time.time - last_speed_change) * speed;
        RotateCamera();
        _link_capacities.Reset();
        RouteHandler.ClearRoutes(_painter);

        // Start logging for this frame.
        _fileWriter.Write($"{framecount}");
        _pathLogger.Write($"{framecount}");

        // Attack attempt.
        var routes = _attacker.Run(graph_on, groundstations.ToList());
        _router.Run(routes, graph_on, _attacker.Target);
        LogRTT(routes);
        
        // Finish logging for this frame.
        _fileWriter.Write("\n");
        _pathLogger.Write("\n");
        _fileWriter.Flush();
        _pathLogger.Flush();

        // Update the scene.
        _painter.UpdateLasers(_constellation.satlist, _constellation.maxsats, speed);
        leftbottom.text = $"Frame {framecount}";
        if (_attacker.Target.Link != null)
        {
            rightbottom.text =
                $"Target Link Capacity: {_link_capacities.GetCapacity(_attacker.Target.Link.SrcNode.Id, _attacker.Target.Link.DestNode.Id)} mbits/sec";
        }
        else
        {
            rightbottom.text = $"No Target Link.";
        }

        // Take a screenshot (if captureMode is enabled)
        if (captureMode)
        {
            captures.CaptureState(cam, leftbottom, framecount); //, leftbottom);
            if (framecount == 50) Terminate();
        }

        framecount++; 
    }

    private void Terminate()
    {
        if (captureMode) SaveVideo();
        PlotData();
        EditorApplication.Exit(0);
    }

    private void SaveVideo()
    {
        var imgHeight = 748 * cam.cam_count;
        var imgWidth = 1504;

        var command =
            // $"ffmpeg -framerate 3 -i {Directory.GetCurrentDirectory()}/Logs/Captures/{_loggingDirectory}/{qualitativeCase}_{targetLinkDirection}_%02d.png -vf \"scale={imgWidth}:{imgHeight}\" -c:v libx265 -preset fast -crf 20 -pix_fmt yuv420p {Directory.GetCurrentDirectory()}/Logs/Captures/{_loggingDirectory}/output.mp4";
            $"ffmpeg -framerate 3 -i {_loggingDirectory}/{caseChoice}_{targetLinkDirection}_%02d.png -vf \"scale={imgWidth}:{imgHeight}\" -c:v libx265 -preset fast -crf 20 -pix_fmt yuv420p {_loggingDirectory}/output.mp4";
        ExecutePowershellCommand(command);
    }

    private void PlotData()
    {
        var command =
            $"python generate_graph.py {_loggingDirectory}/{caseChoice}_{targetLinkDirection}.csv {_loggingDirectory}/{caseChoice}_{targetLinkDirection}_graph.svg";
        ExecutePowershellCommand(command);
    }

    private void ExecutePowershellCommand(string command)
    {
        // TODO: move this stuff somewhere else :)
        var startInfo = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process() { StartInfo = startInfo };

        process.Start();
        var errors = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(5000)) // Kills the process after 5 seconds.
            process.Kill();
        process.Close();

        if (!string.IsNullOrEmpty(errors)) Debug.LogError("PowerShell Errors: " + errors);
    }
}