using System;
using System.Collections.Generic;
using System.Linq;
using Orbits;
using UnityEditor;
using UnityEngine;

namespace Attack
{
    public enum QualitativeCase
    {
        SimpleDemo, // Small Example Case
        Landlocked, // Intra-continental
        Coastal, // Continental coast

        // TODO: add insular example.
        Polar, // High link density
        Equatorial, // Low link density
        TransOrbital, // Across orbits
        IntraOrbital // Within the same orbit
    };

    /// <summary>
    /// Defines the position of the source node in relation to the destination link.
    /// Depends on the general orientation of the attack link. If none is specified,
    /// then the position of the source node should not be selectable.
    /// </summary>
    public enum Direction // DIRECTION
    {
        East, // if target link is horizontal, source node is eastern.
        West, // if target link is horizontal, source node is western.
        North, // if target link is vertical, source node is northern.
        South, // if target link is vertical, source node is southern.
        Any // no preference
    }

    public struct AttackParams
    {
        public static AttackParams Default =>
            new AttackParams(0f, 0f, -1, Direction.Any,
                new List<GameObject>());

        private AttackParams(float latitude, float longitude, int orbitId, Direction direction,
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

        public int OrbitId; // if -1, then there's no specific orbit.

        public Direction Direction; // preferred source node position.
        public List<GameObject> SrcGroundstations; // attacker source ground stations.
    }

    public class AttackCases
    {
        // public void 
        private AttackParams _ctx;

        public AttackCases()
        {
            _ctx = AttackParams.Default;
        }

        /// <summary>
        /// Specify coordinates of the target area based on the attack type choice.
        /// </summary>
        /// <param name="qualitativeCase">Attack case choice.</param>
        public AttackCases SetTargetCoordinates(QualitativeCase qualitativeCase)
        {
            switch (qualitativeCase)
            {
                // TODO: add default case. (the Miami attack)
                case QualitativeCase.SimpleDemo:
                    // Simple example demo
                    _ctx.Latitude = 32.85f;
                    _ctx.Longitude = 80.79f;
                    break;
                case QualitativeCase.Landlocked:
                    // Nebraska, USA
                    _ctx.Latitude = 41.49f;
                    _ctx.Longitude = 99.90f;
                    break;
                case QualitativeCase.Coastal:
                    // Las Vegas, USA
                    _ctx.Latitude = 36.17f;
                    _ctx.Longitude = 115.14f;
                    // _ctx.OrbitId = 16;
                    break;
                case QualitativeCase.Polar:
                    // South of Lake River, Canada
                    _ctx.Latitude = 50f;
                    _ctx.Longitude = 82f;
                    _ctx.OrbitId = 11;
                    break;
                case QualitativeCase.Equatorial:
                    // Quito, Equador
                    // target_lat = 0f;
                    // target_lon = 78f; // good coastal example

                    // // 6°10'02"N 72°38'14"W
                    // _ctx.Latitude = 6.10f;
                    // _ctx.Longitude = 72.38f;
                    _ctx.Latitude = 10.41f;
                    _ctx.Longitude = 67.39f;
                    // 10°41'12"N 67°39'57"W
                    break;
                case QualitativeCase.IntraOrbital:
                    // Denver, USA
                    _ctx.Latitude = 39.73f;
                    _ctx.Longitude = 104.99f;
                    _ctx.OrbitId = 8;
                    break;
                case QualitativeCase.TransOrbital:
                    // Denver, USA
                    _ctx.Latitude = 39.73f;
                    _ctx.Longitude = 104.99f;
                    break;
            }

            return this;
        }

        private List<string>
            LandLocked() 
        {
            switch (_ctx.Direction)
            {
                case Direction.East:
                    return new List<string>
                    {
                        "Oklahoma City",
                        "Phoenix",
                        "Los Angeles",
                        "El Paso",
                        "Houston",
                        "Denver",
                    };
                case Direction.West:
                {
                    return new List<string>
                    {
                        "Toronto",
                        "New York",
                        "Chicago",
                        "Winnipeg",
                        "Edmundston",
                        "Montreal",
                    };
                }
                case Direction.North:
                {
                    return new List<string>
                    {
                        "Las Vegas",
                        "Phoenix",
                        "Los Angeles",
                        "El Paso",
                        "Houston"
                    };
                }
                case Direction.South:
                {
                    return new List<string>
                    {
                        "Toronto",
                        "New York",
                        "Chicago",
                        "Winnipeg",
                        "Edmundston",
                        "Montreal",
                    };
                }
                case Direction.Any: 
                default:
                    return new List<string>
                    {
                        "Toronto",
                        "New York",
                        "Chicago",
                        "Los Angeles",
                        "El Paso",
                        "Houston"
                    };
            }
        }


        private List<string> Coastal()
        {
            switch (_ctx.Direction)
            {
                case Direction.West:
                    return new List<string>
                    {
                        "New York",
                        "Chicago",
                        "Boston",
                        "Washington DC",
                        "Toronto",
                        "Brisay",
                    };
                case Direction.East: 
                    return new List<string>
                    {
                        // West Coast States
                        "San Francisco",
                        
                        // West Pacific
                        "Kagoshima City",
                        "Nishino, Kagoshima",
                        "Ternate City",
                        
                        // Northeast Asia
                        "Saint Petersburg",
                        "Vladivostok"
                        // "Keelung",
                        
                        // Oceania Cities
                        // "Wellington",
                        // "El Paso",
                        // "Phoenix",
                        // "Auckland",
                        // "Honolulu",
                        
                        // ASEAN cities
                        // "Jakarta",
                        // "Manila",
                        // "Quezon City"
                        
                    };
                case Direction.North:
                    _ctx.OrbitId = 16;
                    return new List<string>
                    {
                        "Los Angeles",
                        "El Paso",
                        "San Diego",
                        "Phoenix",
                        "San Antonio",
                        "Denver",
                    };
                case Direction.South:
                    _ctx.OrbitId = 16;
                    return new List<string>
                    {
                        "Seattle",
                        "Vancouver",
                        "San Francisco",
                        "Calgary",
                        "Kagoshima City",
                        "Nishino, Kagoshima"
                    };
                case Direction.Any:
                default:
                    return new List<string>
                    {
                        "Los Angeles",
                        "El Paso",
                        "San Diego",
                        "Phoenix",
                        "Seattle"
                    };
            }
        }

        private List<string> Polar()
        {
            switch (_ctx.Direction)
            {
                case Direction.East:
                    return new List<string>
                    {
                        // Canadian cities
                        "Winnipeg",
                        "Edmonton",
                        "Calgary",
                        "Ottawa",
                        "Vancouver",
                        "Brampton",
                    };
                case Direction.West:
                    return new List<string>
                    {
                        // African cities (Southern/Western region)
                        "Ibadan",
                        "Johannesburg",
                        "Abidjan",
                        "Douala",
                        // Canadian cities
                        "St. John's, Canada",
                        "Edmonton"
                    };
                case Direction.North:
                    return new List<string>
                    {
                        // African cities (Southern/Western region)
                        "Ibadan",
                        "Johannesburg",
                        "Cape Town",
                        "Durban",
                        "Douala",
                        // Canadian cities
                        "St. John's, Canada"
                    };
                case Direction.South:
                    return new List<string>
                    {
                        // Canadian cities
                        "Winnipeg",
                        "Edmonton",
                        "Calgary",
                        "Ottawa",
                        "Vancouver",
                        "Brampton",
                    };
                case Direction.Any:
                default:
                    return new List<string>
                    {
                        // Canadian cities
                        "Winnipeg",
                        "Edmonton",
                        "Calgary",
                        // African cities (Southern region)
                        "Johannesburg",
                        "Cape Town",
                        "Durban",
                    };
            }
        }

        private List<string> Equatorial()
        {
            switch (_ctx.Direction)
            {
                case Direction.South:
                    return new List<string>
                    {
                        // Northern countries (mostly NA)
                        "New York",
                        "Miami",
                        "Caracas",
                        "Maracaibo",
                        "Boston",
                        "Chicago",
                    };
                case Direction.North:
                    return new List<string>
                    {
                        // Southern countries (SA)
                        "Guayaquil",
                        "Lima",
                        "Medellin",
                        "Quito",
                        "Cali"
                    };
                case Direction.East:
                    return new List<string>
                    {
                        "Nishino, Kagoshima",
                        "Hengchung",
                        "Caracas",
                        "Maracaibo",
                        "Medellin",
                        "Quito",
                    };
                    
                case Direction.West:
                    return new List<string>
                    {
                        // Northern countries (mostly NA)
                        "New York",
                        // "Miami",
                        "Caracas",
                        "Maracaibo",
                        // "Boston",
                        // "Chicago",
                    };
                case Direction.Any:
                default:
                    return new List<string>
                    {
                        // South
                        "Guayaquil",
                        "Lima",
                        "Quito",
                        // North
                        "New York",
                        "Miami",
                        "Caracas",
                    };
            }
        }

        private List<string> IntraOrbital()
        {
            return new List<string>
            {
                // top
                "Chicago",
                "Winnipeg",
                // bottom
                "El Paso",
                "Phoenix"
            };
        }

        private List<string> TransOrbital()
        {
            return new List<string>
            {
                // south
                "Fort Worth",
                "Austin",
                // north
                "Denver",
                "San Jose"
            };
        }

        public AttackCases SetLinkDirection(Direction direction)
        {
            _ctx.Direction = direction;

            return this;
        }
        public AttackCases SetSourceGroundstations(QualitativeCase qualitativeCase, 
            GroundstationCollection groundstations)
        {
            var src_gs_names = new List<string>();

            // toDO: Create cities here instead.
            switch (qualitativeCase)
            {
                case QualitativeCase.Landlocked:
                    src_gs_names = LandLocked();
                    break;
                case QualitativeCase.Coastal:
                    src_gs_names = Coastal();
                    break;
                case QualitativeCase.Polar:
                    src_gs_names = Polar();
                    break;
                case QualitativeCase.Equatorial:
                    src_gs_names = Equatorial();
                    break;
                case QualitativeCase.IntraOrbital:
                    src_gs_names = IntraOrbital();
                    break;
                case QualitativeCase.TransOrbital:
                    src_gs_names = TransOrbital();
                    // Attacker source groundstations.
                    break;
                // TODO: add default case. (the Miami attack)
                case QualitativeCase.SimpleDemo:
                default:
                    src_gs_names.AddRange(new List<string>()
                    {
                        "Miami",
                        "Havana"
                    });
                    break;
            }
            foreach (var src_gs_name in src_gs_names) _ctx.SrcGroundstations.Add(groundstations[src_gs_name]);
            return this;
        }

        /// <summary>
        /// Build and return the full attack parameters object.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Throws an exception if the list of source ground stations is empty.</exception>
        public AttackParams Build()
        {
            
            if (!_ctx.SrcGroundstations.Any())
            {

                throw new Exception(
                    "An attacker can't be created without any source ground stations to send traffic from.");
            }

            return _ctx;
        }

        public static void setUpAttackParams(QualitativeCase qualitativeCase, Direction direction,
            GroundstationCollection groundstations, out float target_lat, out float target_lon, out int orbit_id,
            out List<GameObject> src_gs_list)
        {
            target_lat = 0f;
            target_lon = 0f;
            orbit_id = -1;
            var src_gs_names = new List<string>();
            // toDO: Create cities here instead.
            switch (qualitativeCase)
            {
                // TODO: add default case. (the Miami attack)
                case QualitativeCase.SimpleDemo:
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
                case QualitativeCase.Landlocked:
                    // Nebraska, USA
                    target_lat = 41.49f;
                    target_lon = 99.90f;

                    src_gs_names.AddRange(new List<string>
                    {
                        "Toronto",
                        "New York",
                        "Chicago",
                        "Los Angeles",
                        "El Paso",
                        "Houston"
                    });
                    break;
                case QualitativeCase.Coastal:
                    // Las Vegas, USA
                    target_lat = 36.17f;
                    target_lon = 115.14f;
                    orbit_id = 16;

                    src_gs_names.AddRange(new List<string>
                    {
                        "Los Angeles",
                        "El Paso",
                        "San Diego",
                        "Phoenix",
                        "Seattle"
                    });
                    break;
                case QualitativeCase.Polar:
                    // South of Lake River, Canada
                    target_lat = 50f;
                    target_lon = 82f;
                    orbit_id = 11;

                    src_gs_names.AddRange(new List<string>
                    {
                        // Canadian cities (extremity 1)
                        "Winnipeg",
                        "Edmonton",
                        "Calgary"
                        // TODO: put some african cities here. (extremity 2)
                    });
                    break;
                case QualitativeCase.Equatorial:
                    // Quito, Equador
                    target_lat = 0f;
                    target_lon = 78f; // good coastal example

                    // 6°10'02"N 72°38'14"W
                    target_lat = 6.10f;
                    target_lon = 72.38f;


                    src_gs_names.AddRange(new List<string>
                    {
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
                case QualitativeCase.IntraOrbital:
                    // Denver, USA
                    target_lat = 39.73f;
                    target_lon = 104.99f;
                    orbit_id = 8;
                    src_gs_names.AddRange(new List<string>
                    {
                        // top
                        "Chicago",
                        "Winnipeg",
                        // bottom
                        "El Paso",
                        "Phoenix"
                    });
                    break;
                case QualitativeCase.TransOrbital:
                    // Denver, USA
                    target_lat = 39.73f;
                    target_lon = 104.99f;

                    // TODO: Create a set of groundstations that would force trans orbital routing.
                    // TODO: place source groundstations that are perpendicular to this orbit.
                    orbit_id = 8;

                    // Attacker source groundstations.
                    src_gs_names.AddRange(new List<string>
                    {
                        // south
                        "Fort Worth",
                        "Austin",
                        // north
                        "Denver",
                        "San Jose"
                    });
                    break;
            }

            src_gs_list = new List<GameObject>();
            foreach (var src_gs_name in src_gs_names) src_gs_list.Add(groundstations[src_gs_name]);
        }
    }
}