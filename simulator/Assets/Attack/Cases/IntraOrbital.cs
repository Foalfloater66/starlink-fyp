using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{
    public class IntraOrbital : BaseCase
    {
        public IntraOrbital(CityCreator cityCreator, Direction targetLinkDirection,
            GroundstationCollection groundstations, CustomCamera cam) : base(cityCreator, targetLinkDirection,
            groundstations, cam)
        {
        }

        protected override void SetupCameras(CustomCamera cam)
        {
            cam.ViewLandlocked();
        }

        protected override void CreateCities()
        {
            Cities.NACities();
        }

        protected override void CreateTargetCoordinates()
        {
            // Denver, USA
            Ctx.Latitude = 39.73f;
            Ctx.Longitude = 104.99f;
            Ctx.OrbitId = -3;
        }

        protected override List<string> SetSourceGroundstations()
        {
            switch (Ctx.Direction)
            {
                case Direction.East:
                    return new List<string>
                    {
                        "Oklahoma City",
                        "Phoenix",
                        "Los Angeles",
                        "El Paso",
                        "Houston",
                        "Denver"
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
                        "Montreal"
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
                        "Houston",
                        "Denver"
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
                        "Montreal"
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
    }
}