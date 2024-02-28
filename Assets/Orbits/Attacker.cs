using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using System; // Random
using System.Text;
using System.Diagnostics; // Debug.Assert
using UnityEngine.SceneManagement;
using System.Linq; // for ToList();

abstract public class Attacker
{

    /* attacker source groundstations at disposal */
    public List<GameObject> SourceGroundstations { get; set; }

    // TODO: maybe all of this link information should be in a nested class.

    public Node VictimSrcNode { get; protected set; } // victim link source node

    public Node VictimDestNode { get; protected set; } // victim link destination node

    public int LinkCapacityLimit { get; set; } // victim link capacity TODO: should I protect this?

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

    /* update the nodes associated with the target link. */ // TODO: it works even without this. Is it even needed?
    public void UpdateLinkPosition(Node src_node, Node dest_node)
    {
        System.Diagnostics.Debug.Assert(this.VictimSrcNode != null);
        System.Diagnostics.Debug.Assert(this.VictimDestNode != null);

        System.Diagnostics.Debug.Assert(src_node.Id == this.VictimSrcNode.Id);
        System.Diagnostics.Debug.Assert(dest_node.Id == this.VictimDestNode.Id);

        this.VictimSrcNode = src_node;
        this.VictimDestNode = dest_node;
    }

    /* Attempt to attack link by sending paths through the contained source nodes. If successful, returns true. Otherwise, returns false. */
    public bool AttackLink()
    {
        // TODO: write this.

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

    public float Radius { get; private set; }

    public System.IO.StreamWriter summary_logfile;

    /* instantiates the attacker object with a requested victim radius of specified latitude and longitude. */
    public AreaAttacker(float latitude, float longitude, GameObject prefab, Transform transform /* try and make these last 2 vars optional as they seem kind of weird to add here as variables */, float sat0r /* satellite radius from earth centre */, System.IO.StreamWriter summary_logfile, float radius, List<GameObject> src_groundstations)
    {
        this.SourceGroundstations = src_groundstations;
        this.TargetAreaCenterpoint = Vector3.zero;
        this.Radius = radius;
        this.LinkCapacityLimit = 0; // Basically nil if it's not been set.
        this.SetVictimRadius(latitude, longitude, sat0r, prefab, transform);
        this.summary_logfile = summary_logfile;
        System.Diagnostics.Debug.Assert(this.TargetAreaCenterpoint != null);
    }

    /* select a specified centerpoint for the target sphere. If latitude and longitude are 0, generates them randomly. WELL ACTUALLY I CANT DO THAT AND I HAVE TO CHANGE THIS :(*/
    private void SetVictimRadius(float latitude, float longitude, float altitude, GameObject prefab, Transform transform)
    {
        if (latitude == 0)
        {
            latitude = (float)new System.Random().NextDouble() * 180f - 90f; // between -90f and 90f. NEEDS TESTING.
        }
        if (longitude == 0)
        {
            longitude = (float)new System.Random().NextDouble() * 360f - 180f; // between -180f and 180f. NEEDS TESTING.
        }

        // UnityEngine.Debug.Log("Are the latitude and longitude correct?");
        System.Diagnostics.Debug.Assert(latitude > -90 && latitude < 90);
        System.Diagnostics.Debug.Assert(longitude > -180 && longitude < 180);

        // convert from lat, long, and altitude to Vector3 representation.
        /*
        float latRad = latitude * Mathf.Deg2Rad;
        float longRad = longitude * Mathf.Deg2Rad;
        float x = altitude * Mathf.Sin(latRad) * Mathf.Cos(longRad);
        float y = altitude * Mathf.Sin(latRad) * Mathf.Sin(longRad);
        float z = altitude * Mathf.Cos(latRad);
*/



        // GameObject target = GameObject.Instantiate(prefab /* don't know if I need to add a prefab here. I'd rather not */ , new Vector3(x, y, /*-6382.2f*/z), transform.rotation);

        GameObject target = GameObject.Instantiate(prefab, new Vector3(0f, 0f, /*-6382.2f*/-6371.0f - 550f /* TODO: change this distance as well */ ), transform.rotation);
        float long_offset = 20f;
        target.transform.RotateAround(Vector3.zero, Vector3.up, longitude - long_offset);
        Vector3 lat_axis = Quaternion.Euler(0f, -90f, 0f) * target.transform.position;
        target.transform.RotateAround(Vector3.zero, lat_axis, latitude);
        target.transform.SetParent(transform, false);

        this.TargetAreaCenterpoint = target.transform.position;
    }


    /* checks if the input coordinates are within the sphere of attack */
    private bool InTargetArea(Vector3 position)
    {
        summary_logfile.WriteLine(new String('=', 20));
        summary_logfile.WriteLine("TEST POSITION: " + position.x + "; " + position.y + "; " + position.z);
        summary_logfile.WriteLine("TARGET AREA: " + this.TargetAreaCenterpoint.x + "; " + this.TargetAreaCenterpoint.y + "; " + this.TargetAreaCenterpoint.z);

        summary_logfile.WriteLine("DISTANCE BETWEEN THE TWO: " + Vector3.Distance(position, this.TargetAreaCenterpoint));
        summary_logfile.Flush();

        return (Vector3.Distance(position, this.TargetAreaCenterpoint) < this.Radius /* temporary radius */);
    }


    /* Check if the currently selected victim link is still within the target area. Returns true if at least one node is in the target area,
    false otherwise. If the victim link hasn't been set, returns false. */
    public bool HasValidVictimLink()
    {
        if (this.VictimSrcNode != null && this.VictimDestNode != null)
        {
            return (this.InTargetArea(this.VictimSrcNode.Position) || this.InTargetArea(this.VictimDestNode.Position));
        }
        return false;
    }

    private void SelectRandomSrcNode(RouteGraph rg)
    {
        // Pick a random source node.
        Dictionary<int, Node> nodes = new Dictionary<int, Node>(); // index, node 
        int remaining_nodes = rg.nodes.Count();
        for (int i = 0; i < rg.nodes.Count(); i++) nodes.Add(i, rg.nodes[i]);

        while (remaining_nodes > 0)
        {
            int i = new System.Random().Next(rg.nodes.Count());
            Node node = nodes[i];
            if (node == null)
            {
                continue; // already seen.
            }
            if (node.Id > 0 && this.InTargetArea(node.Position))
            {
                this.VictimSrcNode = node;
                break;
            }
            nodes[i] = null;
            remaining_nodes -= 1;
        }
        if (this.VictimSrcNode != null && this.VictimSrcNode.LinkCount == 0)
        {
            this.VictimSrcNode = null; // the source node has no links.
        }
    }

    private Node SelectRandomOutOfTargetDestinationNode(RouteGraph rg)
    {
        Node node = null;
        HashSet<int> explored_indexes = new HashSet<int>();
        int j = 0;

        while (j < this.VictimSrcNode.LinkCount)
        {
            int i = new System.Random().Next(this.VictimSrcNode.LinkCount);
            if (explored_indexes.Contains(i))
            {
                continue;
            }
            node = this.VictimSrcNode.GetNeighbour(this.VictimSrcNode.GetLink(i));
            if (node.Id > 0)
            {
                break;
            }
            explored_indexes.Add(i);
            j++;
        }

        return (node != null && node.Id > 0 ? node : null); // only return valid destination nodes.
    }

    private Node SelectRandomInTargetDestinationNode(RouteGraph rg)
    {

        Dictionary<int, Node> nodes = new Dictionary<int, Node>(); // index, node 
        int remaining_nodes = this.VictimSrcNode.LinkCount;

        for (int i = 0; i < this.VictimSrcNode.LinkCount; i++) nodes.Add(i, this.VictimSrcNode.GetNeighbour(this.VictimSrcNode.GetLink(i)));

        while (remaining_nodes > 0) // prioritise ISL links that are fully contained in the target radius.
        {
            int i = new System.Random().Next(this.VictimSrcNode.LinkCount);
            Node node = nodes[i];
            if (node == null)
            {
                continue;
            }
            if (node.Id > 0 && this.InTargetArea(node.Position))
            {
                return node;
            }
            nodes[i] = null;
            remaining_nodes -= 1;
        }
        return null;
    }

    private void SelectRandomDestinationNode(RouteGraph rg)
    {
        this.VictimDestNode = this.SelectRandomInTargetDestinationNode(rg);
        if (this.VictimDestNode == null)
        {
            this.VictimDestNode = this.SelectRandomOutOfTargetDestinationNode(rg); // select random link which is partially in the target radius
        }
    }

    public String LinkName()
    {
        if (this.VictimSrcNode == null || this.VictimDestNode == null)
        {
            return null;
        }
        return this.VictimSrcNode.Id.ToString() + "-" + this.VictimDestNode.Id.ToString();

    }

    /* randomly select a victim link within the target radius. If no target radius is specified, return a NoVictimError. If success, returns the victim link (src, dest) node. If failure, returns nothing. Failure may occur if no source node was found, or if the source node has no links.
  */
    private void SwitchVictimLink(RouteGraph rg)
    {
        this.SelectRandomSrcNode(rg);
        if (this.VictimSrcNode == null) // might want to just return the source node
        {
            return;
        }
        this.SelectRandomDestinationNode(rg); // might want to just return the destination node.
        if (this.VictimDestNode == null)
        {
            this.VictimSrcNode = null;
        }
    }

    /* Updates the selected links by switching links if the current one is invalid and updates its link capacity according to information coming from the caller */
    public void UpdateLinks(RouteGraph rg, Dictionary<string, int> link_capacity)
    {
        if (!this.HasValidVictimLink())
        {
            this.SwitchVictimLink(rg);
            if (this.VictimSrcNode == null || this.VictimDestNode == null)
            {
                UnityEngine.Debug.Log("Attacker.Update | Could not select a new link.");
            }
        }
        else
        {
            System.Diagnostics.Debug.Log("Attacker.Update | Previous link is still valid."); // I need to highlight the link.
        }

        if (this.HasValidVictimLink())
        {
            if (!link_capacity.ContainsKey(this.LinkName()))
            {
                this.LinkCapacityLimit = SP_Basic_0031.MAX_CAPACITY;
            }
            else
            {
                this.LinkCapacityLimit = link_capacity[link_name];
            }
            UnityEngine.Debug.Log("Attacker.Update | Selected link: " + this.VictimSrcNode.Id + " - " + this.VictimDestNode.Id + " of remaining capacity: " + this.LinkCapacity);
        }
        else
        {
            UnityEngine.Debug.Log("Attacker.Update | Could not find any valid links.");
        }
    }
}

