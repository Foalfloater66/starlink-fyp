using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using System; // Random
using System.Text;
using System.Diagnostics; //  Debug.Assert
using UnityEngine.SceneManagement;
using System.Linq; // for ToList();

abstract public class Attacker
{

    /* attacker source groundstations at disposal */
    public List<GameObject> SourceGroundstations { get; set; }

    public Node VictimSrcNode { get; protected set; } // victim link source node

    public Node VictimDestNode { get; protected set; } // victim link destination node

    /* select n random individual source groundstations for the attacker to use. */
    public List<GameObject> SelectNRandomSourceGSes(uint gs_count, List<GameObject> gs_list)
    {
        uint i = 0;

        // prevent gs_count from exceeding the number of available groundstations.
        if (gs_count > (uint)gs_list.Count)
        {
            gs_count = (uint)gs_list.Count;
        }

        // select n random groundstations.
        Dictionary<GameObject, char> source_gs_list = new Dictionary<GameObject, char>();
        while (i < gs_count)
        {
            GameObject gs = gs_list[new System.Random().Next(0, gs_list.Count)];
            if (!source_gs_list.ContainsKey(gs))
            {
                source_gs_list.Add(gs, '_');
                i += 1;
            }
        }
        this.SourceGroundstations = source_gs_list.Keys.ToList();
        return this.SourceGroundstations;
    }

    /* update the nodes associated with the target link. */
    public void UpdateLinkPosition(Node src_node, Node dest_node)
    {
        System.Diagnostics.Debug.Assert(this.VictimSrcNode != null);
        System.Diagnostics.Debug.Assert(this.VictimDestNode != null);

        System.Diagnostics.Debug.Assert(src_node.Id == this.VictimSrcNode.Id);
        System.Diagnostics.Debug.Assert(dest_node.Id == this.VictimDestNode.Id);

        this.VictimSrcNode = src_node;
        this.VictimDestNode = dest_node;

        bool src_updated = false;
        bool dest_updated = false;
    }

}

public class LinkAttacker : Attacker
{

    /* instantiates the attacker object while manually setting a
    the source and destination nodes of the victim link. */
    public LinkAttacker(Node src_node, Node dest_node)
    {
        this.SourceGroundstations = new List<GameObject>();
        this.VictimSrcNode = src_node;
        this.VictimDestNode = dest_node;
    }

}

public class AreaAttacker : Attacker
{

    public Vector3 TargetAreaCenterpoint { get; private set; }

    public System.IO.StreamWriter summary_logfile;

    /* instantiates the attacker object with a requested victim radius of specified latitude and longitude. */
    public AreaAttacker(float latitude, float longitude, GameObject prefab, Transform transform /* try and make these last 2 vars optional as they seem kind of weird to add here as variables */, System.IO.StreamWriter summary_logfile)
    {
        this.SourceGroundstations = new List<GameObject>();
        this.TargetAreaCenterpoint = Vector3.zero;
        this.SetVictimRadius(latitude, longitude, prefab, transform);
        this.summary_logfile = summary_logfile;
        System.Diagnostics.Debug.Assert(this.TargetAreaCenterpoint != null);
    }


    /* select a specified centerpoint for the target sphere. If latitude and longitude are not specified, generates them randomly. */
    private void SetVictimRadius(float latitude, float longitude, GameObject prefab, Transform transform)
    {
        if (latitude == null)
        {
            latitude = (float)new System.Random().NextDouble() * 180f - 90f; // between -90f and 90f. NEEDS TESTING.
        }
        if (longitude == null)
        {
            longitude = (float)new System.Random().NextDouble() * 360f - 180f; // between -180f and 180f. NEEDS TESTING.
        }

        System.Diagnostics.Debug.Assert(latitude > -90 && latitude < 90);
        System.Diagnostics.Debug.Assert(longitude > -180 && latitude < 180);

        // convert from lat, long to Vector3 representation.
        GameObject target = GameObject.Instantiate(prefab /* don't know if I need to add a prefab here. I'd rather not */ , new Vector3(0f, 0f, /*-6382.2f*/-6371.0f), transform.rotation);
        float long_offset = 20f;
        target.transform.RotateAround(Vector3.zero, Vector3.up, longitude - long_offset);
        Vector3 lat_axis = Quaternion.Euler(0f, -90f, 0f) * target.transform.position;
        target.transform.RotateAround(Vector3.zero, lat_axis, latitude);
        target.transform.SetParent(transform, false);

        // test
        // CityScript cs = (CityScript)target.GetComponent(typeof(CityScript));
        // cs.longitude = longitude;
        // cs.latitude = latitude;

        this.TargetAreaCenterpoint = target.transform.position;

        // draw the sphere for testing
        // UnityEngine.Debug.DrawLine(Vector3.zero, target.transform.position, Color.yellow);
        // Gizmos.color = Color.yellow;
        // Gizmos.DrawSphere(target.transform.position, 1000f);
    }


    /* checks if the input coordinates are within the sphere of attack */
    private bool InTargetArea(Vector3 position)
    {
        summary_logfile.WriteLine(new String('=', 20));
        summary_logfile.WriteLine("TEST POSITION: " + position.x + "; " + position.y + "; " + position.z);
        summary_logfile.WriteLine("TARGET AREA: " + this.TargetAreaCenterpoint.x + "; " + this.TargetAreaCenterpoint.y + "; " + this.TargetAreaCenterpoint.z);

        summary_logfile.WriteLine("DISTANCE BETWEEN THE TWO: " + Vector3.Distance(position, this.TargetAreaCenterpoint));
        summary_logfile.Flush();

        return (Vector3.Distance(position, this.TargetAreaCenterpoint) < 1300f /* temporary radius */);
    }


    /* Check if the currently selected victim link is still within the target area. Returns true if at least one node is in the target area,
    false otherwise. If the victim link hasn't been set, returns false. */ // UNTESTED
    public bool HasValidVictimLink()
    {
        if (this.VictimSrcNode != null && this.VictimDestNode != null)
        {
            return (this.InTargetArea(this.VictimSrcNode.Position) || this.InTargetArea(this.VictimDestNode.Position));
        }
        return false;
    }

    /* randomly select a victim link within the target radius. If no target radius is specified, return a NoVictimError. If success, returns the victim link (src, dest) node. If failure, returns nothing.
  */ // UNTESTED.
    public (Node, Node) SwitchVictimLink(RouteGraph rg)
    {
        UnityEngine.Debug.Log("I got here 1.");
        foreach (Node node in rg.nodes)
        {
            if (this.InTargetArea(node.Position))
            {
                this.VictimSrcNode = node;
                break;
            }
        }

        UnityEngine.Debug.Log("I got here 2.");

        if (this.VictimSrcNode == null)
        {
            UnityEngine.Debug.Log("It's not working :()");
            return (null, null);
        }

        UnityEngine.Debug.Log("I got here 2.1");

        for (int i = 0; i < this.VictimSrcNode.LinkCount; i++)
        {
            UnityEngine.Debug.Log("Am I able to read the neighbours?");
            Node node = this.VictimSrcNode.GetNeighbour(this.VictimSrcNode.GetLink(i));
            UnityEngine.Debug.Log("Looks like I am.");
            if (this.InTargetArea(node.Position))
            {
                this.VictimDestNode = node;
                break;
            }
        }

        UnityEngine.Debug.Log("I got here 3.");

        if (this.VictimDestNode == null)
        {
            this.VictimDestNode = this.VictimSrcNode.GetNeighbour(this.VictimSrcNode.GetLink(new System.Random().Next()));
        }
        return (this.VictimSrcNode, this.VictimDestNode);
    }


}

