using System.Collections.Generic;
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
using Path = System.IO.Path;

public class Main : MonoBehaviour
{
    // Environment Parameters
    [HideInInspector]
    public float direction = 1f;   // If I make this private, Unity (serialization?) sets this to 0, which breaks the program.
    private readonly float _directionChangeSpeed = 2f;
    private int _frameCount = 0;
    private float _pauseStartTime;
    private const double EarthPeriod = 86400f;
    // Core simulation objects
    private GroundstationCollection _groundstations;
    private GameObject[] _lasers;
    private Satellite[] _nearestSats;   // used for calculating collision distances
    private Node[] _nodes;
    private CityCreator _cityCreator;
    private ScenePainter _painter;
    private Constellation _constellation;
    private LinkCapacityMonitor _linkCapacities;
    private Attacker _attacker;
    private RouteHandler _routeHandler;
    private Router _router;
    // Logging objects
    private Captures _captures;
    private StreamWriter _fileWriter;
    private StreamWriter _pathLogger;
    private string _loggingDirectory;
    
    [Header("Environment")] public CustomCamera cam;

    [Tooltip("Speed 1 is realtime")] public float speed = 1f; // a value of 1 is realtime

    [FormerlySerializedAs("use_isls")] [Tooltip("Enable use of inter-sat lasers")]
    public bool useISLs = true;

    [Header("Objects & Materials")]
    // GameObjects
    public GameObject orbit;
    public GameObject satellite;
    public GameObject laser;
    public GameObject thinLaser;
    public GameObject cityPrefab;
    public Material islMaterial;
    public Material[] laserMaterials;
    public Material[] targetLinkMaterial;  // 1st is link up, 2nd is link down.
    public Material cityMaterial;
    
    // Text
    public Text leftbottom;
    public Text rightbottom;
    public Text topleft;
    public GameObject beamPrefab;
    public GameObject beamPrefab2;

    [Header("Attack Parameters")]
    public CaseChoice caseChoice;
    public Direction targetLinkDirection;
    public float attackRadius = 1000f;

    [HideInInspector] public bool deterministicAttacker; // YES OR NO...?


    [Header("Defence Parameters")] 
    public bool defenceOn = false;

    [Header("Logging")]
    public bool screenshotMode = true;
    

    private CustomCamera InitCamera()
    {
        var camscript = (CustomCamera)cam.GetComponent(typeof(CustomCamera));
        camscript.SetupView();
        return camscript;
    }

    private void InitLogging()
    {
        // Create logging directory.
        _loggingDirectory = $"{Directory.GetCurrentDirectory()}/Logs/Captures/{caseChoice}_{targetLinkDirection}";
        if (Directory.Exists(_loggingDirectory)) Directory.Delete(_loggingDirectory, true);
        Directory.CreateDirectory(_loggingDirectory);
        
        // Save a screenshot of each frame.
        if (screenshotMode)
        {
            _captures = new Captures(_loggingDirectory, $"{caseChoice}_{targetLinkDirection}");
        }
    
        // Record information about snapshots.
        _fileWriter = new StreamWriter(Path.Combine(_loggingDirectory, $"{caseChoice}_{targetLinkDirection}.csv"));
        _fileWriter.WriteLine("FRAME,TARGET LINK,ROUTE COUNT,FINAL CAPACITY");
        
        // Record information about the attack routes selected for each frame.
        _pathLogger = new StreamWriter(Path.Combine(_loggingDirectory, "paths.csv"));
        _pathLogger.WriteLine("FRAME,PATHS");
    }

    private void InitScene()
    {
        // GameObjects
        _groundstations = new GroundstationCollection();
        _cityCreator = new CityCreator(transform, cityPrefab, _groundstations);
        _constellation = new Constellation(transform, orbit, satellite, beamPrefab, beamPrefab2, laser, thinLaser, speed, useISLs);
        // UI Text + Visuals.
        rightbottom.text = "";
        topleft.text = "";
        _painter = new ScenePainter(islMaterial, laserMaterials, targetLinkMaterial, cityMaterial);
    }

    private void InitRoutingFramework()
    {
        // Routing + Stats Monitoring
        _linkCapacities = new LinkCapacityMonitor();
        _routeHandler = new RouteHandler();
        _routeHandler.InitRoute(_constellation.maxsats, _constellation.satlist, _constellation.maxdist, _constellation.km_per_unit);
        _router = new Router(defenceOn, _groundstations, _routeHandler, _painter, _linkCapacities, _constellation, _fileWriter, _constellation.km_per_unit);
    }

    private void Start()
    {
        // Simulation configuration.
        Application.runInBackground = true;
        InitLogging();
        InitScene();
        var camscript = InitCamera();
        var attackerParams = CasesFactory
            .GetCase(caseChoice, _cityCreator, targetLinkDirection, _groundstations, camscript).GetParams();
        InitRoutingFramework();
        _attacker = new Attacker(attackerParams, _constellation.sat0r, attackRadius, transform, cityPrefab, _groundstations,
            _routeHandler, _painter, _linkCapacities, _constellation, _fileWriter, _pathLogger);
        camscript.InitView();
        
        // Give the program enough time to generate all game objects when capture mode is disabled.
        Thread.Sleep(10000); 
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_attacker.Target.Center, _attacker.Target.Radius);
    }

    private void RotateCamera()
    {
        // Update the direction of the orbits
        if (direction < 1f) direction += Time.deltaTime / (_directionChangeSpeed / 2);

        // Move the orbits
        for (var i = 0; i < _constellation.maxorbits; i++)
            _constellation.orbits[i].transform.RotateAround(Vector3.zero, _constellation.orbitaxes[i],
                (float)(-1f * EarthPeriod / _constellation.orbitalperiod[i] * _constellation.simspeed * direction) * Time.deltaTime);
    }
    
    /// UI UPDATE FUNCTIONS /// 

    private void UpdateSceneRTT(List<Routing.Path> routes)
    {
        var rttLog = "RTT: ";
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

    private void UpdateSceneLinkStatus()
    {
        if (_attacker.Target.Link != null)
        {
            rightbottom.text =
                $"Target Link Capacity: {_linkCapacities.GetCapacity(_attacker.Target.Link.SrcNode.Id, _attacker.Target.Link.DestNode.Id)} mbits/sec";
        }
        else
        {
            rightbottom.text = "No Target Link.";
        }
    }

    private void ResetScene()
    {
        RotateCamera();
        _linkCapacities.Reset();
        RouteHandler.ClearRoutes(_painter);
    }
    
    private void UpdateScene(List<Routing.Path> routes)
    {
        _painter.UpdateLasers(_constellation.satlist, _constellation.maxsats, speed);
        UpdateSceneRTT(routes);
        leftbottom.text = $"Frame {_frameCount}";
        UpdateSceneLinkStatus();
    }

    /// <summary>
    /// Update function to run the main routegraph rebuild + attacker + frame recording (if enabled)
    /// </summary>
    private void Update()
    {
        // TODO: DEMO MODE NEEDS TO PAUSE FOR EACH STEP/TAKE A SCREENSHOT.
        ResetScene();

        // Start logging for this frame.
        _fileWriter.Write($"{_frameCount}");
        _pathLogger.Write($"{_frameCount}");

        // Attack attempt.
        var routes = _attacker.Run(_groundstations.ToList());
        _router.Run(routes, _attacker.Target);
        
        // Finish logging for this frame.
        _fileWriter.Write("\n");
        _pathLogger.Write("\n");
        _fileWriter.Flush();
        _pathLogger.Flush();
        
        UpdateScene(routes);
        
        if (screenshotMode) _captures.TakeScreenshot(cam, leftbottom, _frameCount);
        if (screenshotMode && _frameCount == 50) Terminate();
        _frameCount++; 
    }

    private void Terminate()
    {
        PowershellTools.SaveVideo(cam, _loggingDirectory, caseChoice, targetLinkDirection);
        PowershellTools.PlotData(cam, _loggingDirectory, caseChoice, targetLinkDirection);
        EditorApplication.Exit(0);
    }
}