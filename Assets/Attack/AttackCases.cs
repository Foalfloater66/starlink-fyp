using System.Collections.Generic;
using Orbits;
using UnityEditor;
using UnityEngine;

namespace Attack
{
    
    // public enum AttackChoice
    // {
    //     Demo, // Small Example Case
    //     LandlockedUS, // Intra-continental
    //     CoastalUS, // Continental coast
    //     // TODO: add insular example.
    //     Polar, // High link density
    //     Equatorial, // Low link density
    //     TransOrbital, // Across orbits
    //     IntraOrbital, // Within the same orbit
    // };
    
    public static class AttackCases
    {

        public static void setUpAttackParams(AttackChoice attack_choice, GroundstationCollection groundstations, out float target_lat, out float target_lon, out int orbit_id, out List<GameObject> src_gs_list)
        {
            target_lat = 0f;
            target_lon = 0f;
            orbit_id = -1;
            List<string> src_gs_names = new List<string>();
            // toDO: Create cities here instead.
            switch (attack_choice)
            {
                // TODO: add default case. (the Miami attack)
                case AttackChoice.Demo:
                    // Simple example demo between Miami & Chicago
                    target_lat = 32.85f;
                    target_lon = 80.79f;
                    // 32.851087, -80.797884
                    
                    // Attacker source groundstations.
                    src_gs_names.AddRange(new List<string>()
                    {
                        "Miami",
                        "Havana"
                    });
                    break;
                case AttackChoice.LandlockedUS:
                    // Nebraska, USA
                    target_lat = 41.49f;
                    target_lon = 99.90f;
                    
                src_gs_names.AddRange(new List<string>{
                    "Toronto",
                    "New York",
                    "Chicago",
                    "Los Angeles",
                    "El Paso",
                    "Houston"
                });
                    break;
                case AttackChoice.CoastalUS:
                    // Las Vegas, USA
                    target_lat = 36.17f;
                    target_lon = 115.14f;
                    orbit_id = 16;
                    
                    src_gs_names.AddRange(new List<string>{
                        "Los Angeles",
                        "El Paso",
                        "San Diego",
                        "Phoenix",
                        "Seattle"
                    });
                    break;
                case AttackChoice.Polar:
                    // South of Lake River, Canada
                    target_lat = 50f;
                    target_lon = 82f;
                    orbit_id = 11;
                    
                    src_gs_names.AddRange(new List<string>{
                        // Canadian cities (extremity 1)
                        "Winnipeg",
                        "Edmonton",
                        "Calgary"
                        // TODO: put some african cities here. (extremity 2)
                    });
                    break;
                case AttackChoice.Equatorial:
                    // Quito, Equador
                    target_lat = 0f;
                    target_lon = 78f; // good coastal example
                    
                    // 6°10'02"N 72°38'14"W
                    target_lat = 6.10f;
                    target_lon = 72.38f;
                    
                    
                    src_gs_names.AddRange(new List<string>{
                        // South
                        "Guayaquil",
                        "Lima",
                        "Medellin",
                        "Quito",
                        "Cali"
                        // North
                        // "Caracas",
                        // "Maracaibo"
                        });
                    break;
                case AttackChoice.IntraOrbital:
                    // Denver, USA
                    target_lat = 39.73f;
                    target_lon = 104.99f;
                    orbit_id = 8;
                    src_gs_names.AddRange(new List<string>{
                        // top
                        "Chicago",
                        "Winnipeg",
                        // bottom
                        "El Paso", 
                        "Phoenix"
                    });
                    break;
                case AttackChoice.TransOrbital:
                    // Denver, USA
                    target_lat = 39.73f;
                    target_lon = 104.99f;
                    
                    // TODO: Create a set of groundstations that would force trans orbital routing.
                    // TODO: place source groundstations that are perpendicular to this orbit.
                    orbit_id = 8;
                    
                    // Attacker source groundstations.
                    src_gs_names.AddRange(new List<string>{
                        // south
                        "Fort Worth",
                        "Austin",
                        // north
                        "Denver",
                        "San Jose",
                        });
                    break;
            }
            src_gs_list = new List<GameObject>();
            foreach (string src_gs_name in src_gs_names)
            {
                src_gs_list.Add(groundstations[src_gs_name]);
            }
        }
    }
}