using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{
    public class TransOrbital : BaseCase
    {
        public TransOrbital(CityCreator cityCreator, Direction targetLinkDirection,
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
            // Pierre, South Dakota, USA
            Ctx.Latitude = 44.36f;
            Ctx.Longitude = 100.35f;
            Ctx.OrbitId = -2;
        }

        protected override List<string> SetSourceGroundstations()
        {
            switch (Ctx.Direction)
            {
                case Direction.North:
                case Direction.South:
                {
                    // TODO: This doesn't really matter because no target link fulfilling the orientation restriction can be found.
                    return new List<string>
                    {
                        "Oklahoma City",
                        "Phoenix",
                        "Los Angeles",
                        "Toronto",
                        "New York",
                        "Chicago",
                    };
                }
                case Direction.East:
                    return new List<string>
                    {
                        "Vancouver",
                        "Calgary",
                        "Seattle",
                        "Edmonton",
                        "San Jose",
                        "San Francisco",
                    };
                case Direction.West:
                {
                    return new List<string>
                    {
                        "Chicago",
                        "Nashville",
                        "Philadelphia",
                        "Montreal",
                        "Phoenix",
                        "New York"
                    };
                }
                case Direction.Any:
                default:
                    return new List<string>
                    {
                        "Fort Worth",
                        "Austin",
                        "Phoenix",
                        "Los Angeles",
                        "Denver",
                        "San Jose"
                    };
            }
        }
    }
}