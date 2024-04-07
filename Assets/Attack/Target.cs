using Routing;
using UnityEngine;

namespace Attack
{
    public class Target
    {
        public class TargetLink
        {
        /// <value>
        /// Source node of the target link.
        /// </value>
        public Node SrcNode { get; private set; }

        /// <value>
        /// Destination node of the target link.
        /// </value>
        public Node DestNode { get; private set; }

        /// <summary>
        /// Constructor initializing the <c>TargetLink</c> object and its source and destination nodes.
        /// </summary>
        /// <param name="src_node"><c>TargetLink</c> source node.</param>
        /// <param name="dest_node"><c>TargetLink</c> destination node.</param>
        public TargetLink(Node src_node, Node dest_node)
        {
            this.SrcNode = src_node;
            this.DestNode = dest_node;
        }
        }
        
        public Vector3 Centerpoint { get; private set; }
        
        public float Radius { get; private set; }

        public Target()
        {
            
        }
        
        
        /// <summary>
        /// Sets and draws a center for the target area based on provided latitude and longitude coordinates.
        /// </summary>
        /// <param name="latitude">Attack area center latitude coordinates. Must be between -90 and 90 degrees.</param>
        /// <param name="longitude">Attack area center longitude coordinates. Must be between -180 and 180 degrees.</param>
        /// <param name="sat0r">Distance of the satellite from Earth's center.</param>
        /// <param name="altitude">Altitude of satellites from Earth's surface.</param>
        /// <param name="transform"><c>Transform</c> object representing the center of the Earth</param>
        /// <param name="prefab">GameObject representing the attack center.</param> 
        private void SetTargetArea(float latitude, float longitude, float altitude, Transform transform,
            GameObject prefab)
        {
            System.Diagnostics.Debug.Assert(latitude > -90 && latitude < 90,
                "Latitude must be between -90 and 90 degrees.");
            System.Diagnostics.Debug.Assert(longitude > -180 && longitude < 180,
                "Longitude must be between -180 and 180 degrees.");

            // convert from lat, long, and altitude to Vector3 representation.
            GameObject target = Object.Instantiate(prefab, new Vector3(0f, 0f, -altitude), transform.rotation);
            float long_offset = 20f;
            target.transform.RotateAround(Vector3.zero, Vector3.up, longitude - long_offset);
            Vector3 lat_axis = Quaternion.Euler(0f, -90f, 0f) * target.transform.position;
            target.transform.RotateAround(Vector3.zero, lat_axis, latitude);
            target.transform.SetParent(transform, false);
            
            // Check if the cityScript is attached.
            Component[] components = target.GetComponents<Component>();
            foreach (Component component in components)
            {
                Debug.Log(component.GetType().ToString());
            }
            Debug.Log("Tried printing all scripts.");
            
            Centerpoint = target.transform.position;
        }
        
    }
}