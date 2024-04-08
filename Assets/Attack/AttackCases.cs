using System.Collections.Generic;
using Orbits;
using UnityEditor;
using UnityEngine;

namespace Attack
{
    public static class AttackCases
    {
        /// <summary>
        /// Create a list of source groundstations the attacker can send packets from.
        /// </summary>
        /// <param name="attack_choice">Selected attack case.</param>
        /// <param name="groundstations"></param>
        /// <param name="src_gs_list"></param>
        public static void getSourceGroundstations(AttackChoice attack_choice,
            GroundstationCollection groundstations, out List<GameObject> src_gs_list)
        {
            List<string> src_gs_names;

            switch (attack_choice)
            {
                case AttackChoice.Demo:
                    src_gs_names = new List<string>
                    {
                        "Toronto",
                        "New York",
                        "Miami",
                        "Chicago",
                        "Denver",
                        "Houston"
                    };
                    break;
                case AttackChoice.TranscontinentalUS:
                    src_gs_names = new List<string>
                    {
                        "Toronto",
                        "New York",
                        "Chicago",
                        "Los Angeles",
                        "El Paso",
                        "Houston"
                    };
                    break;
                case AttackChoice.CoastalUS:
                    src_gs_names = new List<string>
                    {
                        "Los Angeles",
                        "El Paso",
                        "San Diego",
                        "Phoenix",
                    };
                    break;
                case AttackChoice.Polar:
                    src_gs_names = new List<string>
                    {
                        // TODO: add european cities to the map.
                        "Winnipeg",
                        "Seattle",
                        "Oklahoma City",
                        "Edmonton",
                        
                    };
                    break;
                case AttackChoice.Equatorial:
                    src_gs_names = new List<string>
                    {
                        ""
                    };
                    break;
                case AttackChoice.IntraOrbital:
                    src_gs_names = new List<string>
                    {
                        ""
                    };
                    break;
                case AttackChoice.TransOrbital:
                    src_gs_names = new List<string>
                    {
                        ""
                    };
                    break;
                default:
                    src_gs_names = new List<string>();
                    break;
            }

            src_gs_list = new List<GameObject>();
            foreach (string src_gs_name in src_gs_names)
            {
                src_gs_list.Add(groundstations[src_gs_name]);
            }
        }
        
        public static void getTargetCoordinates(AttackChoice attack_choice, out float target_lat, out float target_lon)
        {
            target_lat = 0f;
            target_lon = 0f;
            switch (attack_choice)
            {
                // TODO: add default case. (the Miami attack)
                case AttackChoice.Demo:
                    // Simple example demo between Miami & Chicago
                    target_lat = 32f;
                    target_lon = 82f;
                    break;
                case AttackChoice.TranscontinentalUS:
                    // Nebraska, USA
                    target_lat = 41.49f;
                    target_lon = 99.90f;
                    break;
                case AttackChoice.CoastalUS:
                    // San Francisco, USA
                    target_lat = 37.7749f;
                    target_lon = 122.4194f;
                    break;
                case AttackChoice.Polar:
                    // Lake River, Canada
                    target_lat = 54f;
                    target_lon = 82f;
                    break;
                case AttackChoice.Equatorial:
                    // Quito, Equador
                    target_lat = 0f;
                    target_lon = 78f;
                    break;
                case AttackChoice.IntraOrbital:
                    // TODO: Create a set of groundstations that would force intra orbital routing.
                    target_lat = 0f;
                    target_lon = 0f;
                    break;
                case AttackChoice.TransOrbital:
                    // TODO: Create a set of groundstations that would force trans orbital routing.
                    target_lat = 0f;
                    target_lon = 0f;
                    break;
            }
        }
    }
}