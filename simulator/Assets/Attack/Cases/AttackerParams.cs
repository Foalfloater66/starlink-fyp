using System.Collections.Generic;
using UnityEngine;

namespace Attack.Cases
{
    public struct AttackerParams
    {
        public static AttackerParams Default =>
            new AttackerParams(0f, 0f, -1, Direction.Any, new List<GameObject>());

        private AttackerParams(float latitude, float longitude, int orbitId, Direction direction,
            List<GameObject> srcGroundstations)
        {
            Latitude = latitude;
            Longitude = longitude;
            OrbitId = orbitId;
            Direction = direction;
            SrcGroundstations = srcGroundstations;
        }

        public float Latitude; // target area latitude.
        public float Longitude; // target area longitude.

        // >=0: both link nodes must belong to a specific orbit.
        // -1: no link specified.
        // -2: link nodes must not share the same orbit.
        // -3: both link nodes must share the same unspecified orbit.
        public int OrbitId; 

        public Direction Direction; // preferred source node position.
        public List<GameObject> SrcGroundstations; // attacker source ground stations.
    }
}