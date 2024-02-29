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

public class Path : HeapNode
{
    // this should be in routing.
    public List<Node> nodes = new List<Node>();

    public GameObject startcity;

    public GameObject endcity;

    public Path(GameObject startcity, GameObject endcity)
    {
        this.startcity = startcity;
        this.endcity = endcity;
    }
}

public class Attacker
{

    public class TargetLink
    {
        public Node SrcNode { get; protected set; }
        public Node DestNode { get; protected set; }

        public string Name { get; private set; }
        public int Capacity { get; set; } // victim link capacity TODO: should I protect this?

        public TargetLink(Node src_node, Node dest_node, int capacity)
        {
            this.SrcNode = src_node;
            this.DestNode = dest_node;
            this.Name = SrcNode.Id.ToString() + "-" + SrcNode.Id.ToString();
            this.Capacity = capacity;
        }
    }

    /* attacker source groundstations at disposal */
    public List<GameObject> SourceGroundstations { get; set; }

    public Vector3 TargetAreaCenterpoint { get; private set; }

    public float Radius { get; private set; }

    public TargetLink Link { get; private set; }

    public System.IO.StreamWriter summary_logfile; // TODO: move it here.

    /* instantiates the attacker object with a requested victim radius of specified latitude and longitude. */
    public Attacker(float latitude, float longitude, GameObject prefab, Transform transform /* try and make these last 2 vars optional as they seem kind of weird to add here as variables */, float sat0r /* satellite radius from earth centre */, System.IO.StreamWriter summary_logfile, float radius, List<GameObject> src_groundstations)
    {
        this.SourceGroundstations = src_groundstations;
        this.TargetAreaCenterpoint = Vector3.zero;
        this.Radius = radius;
        this.SetVictimRadius(latitude, longitude, sat0r, prefab, transform);
        this.Link = null;
        this.summary_logfile = summary_logfile;
        System.Diagnostics.Debug.Assert(this.TargetAreaCenterpoint != null);
    }

    /* Check if the currently selected victim link is still within the target area. Returns true if at least one node is in the target area,
    false otherwise. If the victim link hasn't been set, returns false. */
    public bool HasValidVictimLink()
    {
        return (this.Link != null && (this.InTargetArea(this.Link.SrcNode.Position) || this.InTargetArea(this.Link.DestNode.Position)));
    }

    /* Get the shorted route between src_gs & dest_gs. If a route containing the target link is found, returen the route graph with the length of the route. Otherwise, return null. */
    public Path ExtractAttackRoute(RouteGraph rg, GameObject src_gs, GameObject dest_gs)
    {
        // TODO: Optimise the extracting of a route.
        System.Diagnostics.Debug.Assert(this.Link != null, "ExtractAttackRoute | No attacker link has been set.");

        Path route = new Path(src_gs, dest_gs);
        Node rn = rg.endnode;
        int startsatid = 0, endsatid = -1;
        int id = -4;
        bool viable_route = false; // can the target link be attacked with this route?

        while (true)
        {
            if (rn == rg.startnode)
            {
                startsatid = id;
                break;
            }

            id = rn.Id;

            if (endsatid == -1 && id >= 0)
            {
                endsatid = id;
            }
            if (id >= 0)
            {
                if (route.nodes.Last().Id == this.Link.SrcNode.Id && id == this.Link.DestNode.Id)
                {
                    viable_route = true;
                }
            }
            rn = rn.Parent;

            if (rn == null) // this route is incomplete.
            {
                return null;
            }
            route.nodes.Add(rn);
        }
        List<string> path = new List<string>();
        foreach (Node node in route.nodes)
        {
            path.Add(node.Id.ToString());
        }
        UnityEngine.Debug.Log("ExtractAttackRoute: found path of length " + route.nodes.Count + ": " + string.Join(" ", path));

        if (viable_route)
        {
            return route;
        }
        return null;
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
        return (Vector3.Distance(position, this.TargetAreaCenterpoint) < this.Radius);
    }

    private Node SelectRandomSrcNode(RouteGraph rg)
    {
        Node node = null;
        Dictionary<int, Node> nodes = new Dictionary<int, Node>(); // index, node 
        int remaining_nodes = rg.nodes.Count();
        for (int i = 0; i < rg.nodes.Count(); i++) nodes.Add(i, rg.nodes[i]);

        while (remaining_nodes > 0)
        {
            int i = new System.Random().Next(rg.nodes.Count());
            node = nodes[i];
            if (node == null)
            {
                continue; // already seen.
            }
            if (node.Id > 0 && this.InTargetArea(node.Position))
            {
                // src_node = node;
                break;
            }
            nodes[i] = null;
            remaining_nodes -= 1;
        }
        return node; // the node might not have any links!
    }

    private Node SelectRandomOutOfTargetDestinationNode(Node src_node, RouteGraph rg)
    {
        Node node = null;
        HashSet<int> explored_indexes = new HashSet<int>();
        int j = 0;

        while (j < src_node.LinkCount)
        {
            int i = new System.Random().Next(src_node.LinkCount);
            if (explored_indexes.Contains(i))
            {
                continue;
            }
            node = src_node.GetNeighbour(src_node.GetLink(i));
            if (node.Id > 0)
            {
                break;
            }
            explored_indexes.Add(i);
            j++;
        }

        return (node != null && node.Id > 0 ? node : null); // only return valid destination nodes.
    }

    private Node SelectRandomInTargetDestinationNode(Node src_node, RouteGraph rg)
    {

        Dictionary<int, Node> nodes = new Dictionary<int, Node>(); // index, node 
        int remaining_nodes = src_node.LinkCount;

        for (int i = 0; i < src_node.LinkCount; i++) nodes.Add(i, src_node.GetNeighbour(src_node.GetLink(i)));

        while (remaining_nodes > 0) // prioritise ISL links that are fully contained in the target radius.
        {
            int i = new System.Random().Next(src_node.LinkCount);
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

    private Node SelectRandomDestinationNode(Node src_node, RouteGraph rg)
    {
        Node dest_node = this.SelectRandomInTargetDestinationNode(src_node, rg);
        if (dest_node == null)
        {
            return this.SelectRandomOutOfTargetDestinationNode(src_node, rg); // select random link which is partially in the target radius
        }
        return dest_node;
    }

    /* randomly select a victim link within the target radius. If no target radius is specified, return a NoVictimError. If success, returns the victim link (src, dest) node. If failure, returns nothing. Failure may occur if no source node was found, or if the source node has no links.
    */
    private void ChangeVictimLink(RouteGraph rg, int max_capacity)
    {
        Node src_node = this.SelectRandomSrcNode(rg);
        if (src_node != null)
        {
            Node dest_node = this.SelectRandomDestinationNode(src_node, rg); // TODO: just select out of neighbours. I don't really need the routegraph at this point.
            if (dest_node != null)
            {
                this.Link = new Attacker.TargetLink(src_node, dest_node, max_capacity);
                return;
            }
        }
        this.Link = null;
    }

    /* Updates the selected links by switching links if the current one is invalid and updates its link capacity according to information coming from the caller */
    public void UpdateLinks(RouteGraph rg, int max_capacity)
    {

        if (!this.HasValidVictimLink())
        {
            this.ChangeVictimLink(rg, max_capacity);
            if (this.Link == null)
            {
                UnityEngine.Debug.Log("Attacker.Update | Could not find any valid link.");
            }
            else
            {
                UnityEngine.Debug.Log("Attacker.Update | Changed link: " + this.Link.SrcNode.Id + " - " + this.Link.DestNode.Id + " of remaining capacity: " + this.Link.Capacity);
            }
        }
        else
        {
            UnityEngine.Debug.Log("Attacker.Update | Previous link is still valid.");
        }
    }
}

