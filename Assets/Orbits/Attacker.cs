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

    /* attacker source groundstations at disposal */
    public List<GameObject> SourceGroundstations { get; set; }

    public Vector3 TargetAreaCenterpoint { get; private set; }

    public float Radius { get; private set; }

    public System.IO.StreamWriter summary_logfile; // TODO: move it here.

    public Node VictimSrcNode { get; protected set; } // victim link source node

    public Node VictimDestNode { get; protected set; } // victim link destination node

    public int LinkCapacityLimit { get; set; } // victim link capacity TODO: should I protect this?

    public String LinkName() // TODO: move this from LinkName function to a function that it only can update the source ndoe.
    {
        if (this.VictimSrcNode == null || this.VictimDestNode == null)
        {
            return null;
        }
        return this.VictimSrcNode.Id.ToString() + "-" + this.VictimDestNode.Id.ToString();
    }

    /* Compute the shortest route between src_gs, dest_gs pair. If a route containing the target link is found, returen the route graph with the length of the route. Otherwise, return null. */ // TODO: is there a more efficient way of computing the length? maybe through the djikstra function? not doing that now though.
    public Path ExtractAttackRoute(RouteGraph rg, GameObject src_gs, GameObject dest_gs)
    {
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
                if (route.nodes.Last().Id == this.VictimDestNode.Id && id == this.VictimSrcNode.Id)
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

    // public BinaryHeap<Path> FindAttackRoutes(RouteGraph rg, List<GameObject> dest_groundstations, )
    // {
    //     BinaryHeap<Path> attack_routes = new BinaryHeap<Path>(dest_groundstations.Count * this.SourceGroundstations.Count); // priority queue <routegraph, routelength>
    //     foreach (GameObject src_gs in this.SourceGroundstations)
    //     {
    //         foreach (GameObject dest_gs in dest_groundstations)
    //         {
    //             if (dest_gs == src_gs)
    //             {
    //                 continue;
    //             }
    //             // TODO: should I clear the route graph?
    //             rg = SP_basic_0031.BuildRouteGraph(rg, src_gs, dest_gs, SP_basic_0031.maxdist, SP_basic_0031.margin);
    //             rg.ComputeRoutes();

    //             List<Path> path = this.ExtractAttackRoute(rg, src_gs, dest_gs);
    //             if (path != null)
    //             {
    //                 attack_routes.Enqueue(path);
    //             }
    //         }
    //     }
    //     // TODO: add the routes in a pq
    //     return attack_routes;
    // }

    /* Attempt to attack link by sending paths through the contained source nodes. If successful, returns true. Otherwise, returns false. */
    // public void AttackLink(RouteGraph rg, List<GameObject> dest_groundstations)
    // {
    //     // TODO: write this.
    //     PriorityQueue<RouteGraph, int> attack_routes = this.FindAttackRoutes(rg, dest_groundstations);
    // }

    /* instantiates the attacker object with a requested victim radius of specified latitude and longitude. */
    public Attacker(float latitude, float longitude, GameObject prefab, Transform transform /* try and make these last 2 vars optional as they seem kind of weird to add here as variables */, float sat0r /* satellite radius from earth centre */, System.IO.StreamWriter summary_logfile, float radius, List<GameObject> src_groundstations)
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

    /* update the nodes associated with the target link. */ // TODO: it works even without this. Is it even needed?
    public void UpdateLinkPosition(RouteGraph rg)
    {
        System.Diagnostics.Debug.Assert(this.VictimSrcNode != null);
        System.Diagnostics.Debug.Assert(this.VictimDestNode != null);

        this.VictimSrcNode = rg.GetNode(this.VictimSrcNode.Id);
        this.VictimDestNode = rg.GetNode(this.VictimDestNode.Id);
    }


    /* Updates the selected links by switching links if the current one is invalid and updates its link capacity according to information coming from the caller */
    public void UpdateLinks(RouteGraph rg, Dictionary<string, int> link_capacity, int max_capacity)
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
            UnityEngine.Debug.Log("Attacker.Update | Previous link is still valid of coords " + this.VictimSrcNode.Position); // I need to highlight the link.
        }

        if (this.HasValidVictimLink())
        {
            if (!link_capacity.ContainsKey(this.LinkName()))
            {
                this.LinkCapacityLimit = max_capacity;
            }
            else
            {
                this.LinkCapacityLimit = link_capacity[this.LinkName()];
            }
            UnityEngine.Debug.Log("Attacker.Update | Selected link: " + this.VictimSrcNode.Id + " - " + this.VictimDestNode.Id + " of remaining capacity: " + this.LinkCapacityLimit);
        }
        else
        {
            UnityEngine.Debug.Log("Attacker.Update | Could not find any valid links.");
        }
    }
}

