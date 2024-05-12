using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{
    public class CoastalCase : BaseCase
    {
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