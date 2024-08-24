/* C138 Final Year Project 2023-2024 */

using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{
    public class Polar : BaseAttribute
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
                        "Winnipeg",
                        "Edmonton",
                        "Calgary",
                        "Ottawa",
                        "Brampton"
                    };
                case Direction.West:
                    return new List<string>
                    {
                        "Ibadan",
                        "Abidjan",
                        "Douala",
                        "St. John's, Canada",
                        "Edmonton"
                    };
                case Direction.North:
                    return new List<string>
                    {
                        "Ibadan",
                        "Cape Town",
                        "Durban",
                        "Douala",
                        "St. John's, Canada"
                    };
                case Direction.South:
                    return new List<string>
                    {
                        "Winnipeg",
                        "Edmonton",
                        "Calgary",
                        "Ottawa",
                        "Brampton"
                    };
                case Direction.Any:
                default:
                    return new List<string>
                    {
                        "Winnipeg",
                        "Edmonton",
                        "Calgary",
                        "Cape Town",
                        "Durban"
                    };
            }
        }
    }
}