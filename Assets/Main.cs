﻿using System;
using System.Collections.Generic;
using System.IO;
using Attack;
using Attack.Cases;
using Automation;
using Logging;
using Orbits;
using Orbits.Satellites;
using Routing;
using UnityEngine;
using Scene;
using UnityEditor;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utilities;
using Utilities.Logging;
using ILogger = Utilities.Logging.ILogger;

public class Main : MonoBehaviour
{
    // Environment Parameters
    [HideInInspector] public float
        direction = 1f; // If I make this private, Unity (serialization?) sets this to 0, which breaks the program.

    private readonly float _directionChangeSpeed = 2f;
    private int _frameCount = 0;
    private float _pauseStartTime;

    private const double EarthPeriod = 86400f;

    // Core simulation objects
    private GroundstationCollection _groundstations;
    private GameObject[] _lasers;
    private Satellite[] _nearestSats; // used for calculating collision distances
    private Node[] _nodes;
    private CityCreator _cityCreator;
    private ScenePainter _painter;
    private Constellation _constellation;
    private LinkCapacityMonitor _linkCapacities;
    private Attacker _attacker;
    private RouteGraph _rg;

    private Router _router;

    // Logging objects
    private Captures _captures;
    private string _loggingDirectory;
    private ILogger _attackLogger;
    private ILogger _pathLogger;
    private ILogger _latencyLogger;
    [FormerlySerializedAs("exp")] [HideInInspector] public Runner runner;

    [Header("Environment")] public CustomCamera cam;

    [Tooltip("Speed 1 is realtime")] public float speed = 10f; // a value of 1 is realtime

    [FormerlySerializedAs("use_isls")]
    [FormerlySerializedAs("use_ISLs")]
    [FormerlySerializedAs("useISLs")]
    [Tooltip("Enable use of inter-sat lasers")]
    public bool useIsls = true;

    [Header("Objects & Materials")]
    // GameObjects
    public GameObject orbit;

    public GameObject satellite;
    public GameObject laser;
    public GameObject thinLaser;
    public GameObject cityPrefab;
    public Material islMaterial;
    public Material[] laserMaterials;
    public Material[] targetLinkMaterial; // 1st is link up, 2nd is link down.
    public Material cityMaterial;

    // Text
    public Text leftBottomText;
    public Text rightBottomText;
    public Text topLeftText;
    public GameObject beamPrefab;
    public GameObject beamPrefab2;

    [Header("Attack Parameters")] public CaseChoice caseChoice;
    public Direction targetLinkDirection;
    public float attackRadius = 1000f;

    // TODO: complete this later.
    [HideInInspector] [Tooltip("Whether the attacker assumes deterministic routing or not.")]
    public bool deterministicAttacker = true; // YES OR NO...?

    [Header("Defence Parameters")] public int rmax = 1;

    [Tooltip("Apply shortest route randomisation mechanism.")]
    public bool defenceOn = false;

    [Header("Logging")] public bool logScreenshots = false;
    public bool logAttack = false;
    public bool logRTT = false;

    [Tooltip(
        "Maximum number of frames to compute before terminating the simulator. Only takes effect when any logging is enabled.")]
    public int maxFrames = 50;

    [HideInInspector] public int runId = 0; // For tracking (helpful for experiments on random behaviour)

    private CustomCamera InitCamera()
    {
        var camscript = (CustomCamera)cam.GetComponent(typeof(CustomCamera));
        camscript.SetupView();
        return camscript;
    }

    private void InitLogging()
    {
        // Create logging directory.
        string status;
        if (rmax != 1 && defenceOn) status = rmax.ToString();
        else status = "OFF";
        string filename = $"{caseChoice}_{targetLinkDirection}_{status}_{runId:D3}";
        _loggingDirectory = $"{Directory.GetCurrentDirectory()}/Logs/Captures/{filename}";
        if (Directory.Exists(_loggingDirectory)) Directory.Delete(_loggingDirectory, true);
        Directory.CreateDirectory(_loggingDirectory);

        if (logScreenshots)
        {
            _captures = new Captures(_loggingDirectory);
        }

        if (logAttack)
        {
            _attackLogger = new AttackLogger(_loggingDirectory, _linkCapacities);
            _pathLogger = new PathLogger(_loggingDirectory, _groundstations);
        }

        if (logRTT)
        {
            _latencyLogger = new LatencyLogger(_loggingDirectory);
        }
    }

    private void InitScene()
    {
        // GameObjects
        _groundstations = new GroundstationCollection();
        _cityCreator = new CityCreator(transform, cityPrefab, _groundstations);
        _constellation = new Constellation(transform, orbit, satellite, beamPrefab, beamPrefab2, laser, thinLaser,
            speed, useIsls);
        // UI Text + Visuals.
        rightBottomText.text = "";
        topLeftText.text = "";
        _painter = new ScenePainter(islMaterial, laserMaterials, targetLinkMaterial, cityMaterial);
    }

    private void InitRoutingFramework()
    {
        // Routing + Stats Monitoring
        _linkCapacities = new LinkCapacityMonitor();
        _rg = new RouteGraph();
        _rg.InitRoute(_constellation.maxsats, _constellation.satlist, _constellation.maxdist,
            _constellation.km_per_unit);
        _router = new Router(defenceOn, _groundstations, _rg, _painter, _linkCapacities, _constellation,
            _constellation.km_per_unit, rmax);
    }

    public void Start()
    {
        // Simulation configuration.
        Application.runInBackground = true;

        if (!defenceOn)
        {
            runId = 0;
        }

        Debug.Log($"Run Parameters: " +
                  $"CASE CHOICE: {caseChoice};" +
                  $"TARGET LINK DIRECTION: {targetLinkDirection}; " +
                  $"ATTACK RADIUS: {attackRadius}; " +
                  $"DEFENCE ON: {defenceOn}; " +
                  $"RMAX: {rmax}; " +
                  $"LOG ATTACK: {logAttack}; " +
                  $"LOG RTT: {logRTT}; " +
                  $"MAX FRAMES: {maxFrames};");

        InitScene();
        var camscript = InitCamera();
        var attackerParams = CasesFactory
            .GetCase(caseChoice, _cityCreator, targetLinkDirection, _groundstations, camscript).GetParams();
        InitRoutingFramework();
        _attacker = new Attacker(attackerParams, _constellation.sat0r, attackRadius, transform, cityPrefab,
            _groundstations,
            _rg, _painter, _linkCapacities, _constellation);
        if (logAttack || logRTT || logScreenshots)
        {
            InitLogging();
        }

        camscript.InitView();
        Update();
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
                (float)(-1f * EarthPeriod / _constellation.orbitalperiod[i] * _constellation.simspeed * direction) *
                Time.deltaTime);
    }

    private void UpdateSceneRTT(List<float> rttList)
    {
        var rttLog = "RTT: ";
        for (int idx = 0; idx < rttList.Count; idx++)
        {
            float rtt = rttList[idx];
            if (Single.IsNaN(rtt))
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

        topLeftText.text = rttLog;
    }

    private void UpdateSceneLinkStatus()
    {
        if (_attacker.Target.Link != null)
        {
            rightBottomText.text =
                $"Target Link Capacity: {_linkCapacities.GetCapacity(_attacker.Target.Link.SrcNode.Id, _attacker.Target.Link.DestNode.Id) / 1000} Gbits/snapshot";
        }
        else
        {
            rightBottomText.text = "No Target Link.";
        }
    }

    private void ResetScene()
    {
        RotateCamera();
        _linkCapacities.Reset();
        _rg.ClearRoutes(_painter);
    }

    private void UpdateScene(List<float> rttList)
    {
        _painter.UpdateLasers(_constellation.satlist, _constellation.maxsats, speed);
        UpdateSceneRTT(rttList);
        leftBottomText.text = $"Frame {_frameCount}";
        UpdateSceneLinkStatus();
    }

    private List<float> ExtractRTT(List<Route> routes)
    {
        // var rttLog = "RTT: ";
        List<float> RTT = new List<float>();
        for (int idx = 0; idx < routes.Count; idx++)
        {
            float rtt = routes[idx].GetRTT(_constellation.km_per_unit);
            if (rtt > 2000f)
            {
                RTT.Add(Single.NaN);
            }
            else
            {
                RTT.Add(rtt);
            }
        }

        return RTT;
    }

    /// <summary>
    /// Update function to run the main routegraph rebuild + attacker + frame recording (if enabled)
    /// </summary>
    public void Update()
    {
        // TODO: DEMO MODE NEEDS TO PAUSE FOR EACH STEP/TAKE A SCREENSHOT.
        ResetScene();

        // // Attempt an attack on the network.
        var routes = _attacker.Run(_groundstations.ToList());
        _router.Run(routes, _attacker.Target);

        List<float> rttList = ExtractRTT(routes);
        UpdateScene(rttList);

        LoggingContext ctx = new LoggingContext()
        {
            Target = _attacker.Target,
            Routes = routes,
            RTT = rttList
        };
        if (logAttack)
        {
            _attackLogger.LogEntry(_frameCount, ctx);
            _pathLogger.LogEntry(_frameCount, ctx);
        }

        if (logRTT)
        {
            _latencyLogger.LogEntry(_frameCount, ctx);
        }

        if (logScreenshots) _captures.TakeScreenshot(cam, leftBottomText, _frameCount);

        // Terminate
        if (_frameCount == maxFrames) Terminate();

        _frameCount++;
    }

    private void Terminate()
    {
        if (logScreenshots)
        {
            PowershellTools.SaveVideo(cam, _loggingDirectory, caseChoice, targetLinkDirection, defenceOn, rmax);
        }

        if (logRTT)
        {
            _latencyLogger.Save();
        }

        if (runner && runner.experimentMode && runner.Experiments.Count != 0)
        {
            Debug.Log("Switching Scenes.");
            runner.Next();
        }
        else
        {
            Debug.Log("Run completed.");
            EditorApplication.Exit(0);
        }
    }
}