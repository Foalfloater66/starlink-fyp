using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{
    public class SimpleDemo : BaseCase
    {
        public SimpleDemo(CityCreator cityCreator, Direction targetLinkDirection,
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
            Cities.DemoCities();
        }

        protected override void CreateTargetCoordinates()
        {
            // Miami-New York area
            Ctx.Latitude = 32.85f;
            Ctx.Longitude = 80.79f;
        }

        protected override List<string> SetSourceGroundstations()
        {
            return new List<string>()
            {
                "Miami",
                "Havana"
            };
        }
    }
}