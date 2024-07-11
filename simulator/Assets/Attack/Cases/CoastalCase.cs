using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{
    public class CoastalCase : BaseCase
    {
        // Note about the coastal case: The coastal case focuses on perpendicular versus parallel. So north is north RELATIVE
        // to the coastline, NOT to the world.
        public CoastalCase(CityCreator cityCreator, Direction targetLinkDirection,
            GroundstationCollection groundstations, CustomCamera cam) : base(cityCreator, targetLinkDirection,
            groundstations, cam)
        {
        }

        protected override void SetupCameras(CustomCamera cam)
        {
            cam.ViewCoastal(Ctx.Direction);
        }

        protected override void CreateCities()
        {
            Cities.NonASEANCities();
            Cities.ASEANCities();
            Cities.WPacific();
            if (Ctx.Direction == Direction.East)
                Cities.NorthAsiaCities();
            else
                Cities.OceaniaCities();
            Cities.NACities();
        }

        protected override void CreateTargetCoordinates()
        {
            // Las Vegas, USA
            Ctx.Latitude = 36.17f;
            Ctx.Longitude = 115.14f;
        }

        protected override List<string> SetSourceGroundstations()
        {
            switch (Ctx.Direction)
            {
                case Direction.West:
                    // Perpendicular westwards
                    Ctx.OrbitId = 8;
                    return new List<string>
                    {
                        "New York",
                        "Chicago",
                        "Boston",
                        "Washington DC",
                        "Toronto",
                        "Brisay"
                    };
                case Direction.East:
                    // Perpendicular eastwards
                    Ctx.OrbitId = 8;
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
                    };
                case Direction.North:
                    // Parallel northwards
                    // North is in reality eastwards, but northwards when parallel to the coastline.
                    Ctx.Direction = Direction.East; 
                    Ctx.OrbitId = 16;
                    return new List<string>
                    {
                        "Los Angeles",
                        "El Paso",
                        "San Diego",
                        "Phoenix",
                        "San Antonio",
                        "Denver"
                    };
                case Direction.South:
                    // Parallel southwards
                    // South is in reality westwards, but southwards when parallel to the coastline.
                    Ctx.Direction = Direction.West;
                    Ctx.OrbitId = 16;
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
    }
}