using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{
    // TODO: finish this example case.
    public class Polar : BaseCase
    {
        public Polar(CityCreator cityCreator, Direction targetLinkDirection, GroundstationCollection groundstations,
            CustomCamera cam) : base(cityCreator, targetLinkDirection, groundstations, cam)
        {
        }

        protected override void SetupCameras(CustomCamera cam)
        {
            cam.ViewPolar();
        }

        protected override void CreateCities()
        {
            Cities.CANCities();
            Cities.AFCities();
        }

        protected override void CreateTargetCoordinates()
        {
            // South of Lake River, Canada
            Ctx.Latitude = 50f;
            Ctx.Longitude = 82f;
            Ctx.OrbitId = 11;
        }

        protected override List<string> SetSourceGroundstations()
        {
            switch (Ctx.Direction)
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
                        "Brampton"
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
                        "Brampton"
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
                        "Durban"
                    };
            }
        }
    }
}