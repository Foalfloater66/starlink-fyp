using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{
    public class IntraOrbital : BaseCase
    {
        public IntraOrbital(CityCreator cityCreator, Direction targetLinkDirection, GroundstationCollection groundstations, CustomCamera cam) : base(cityCreator, targetLinkDirection, groundstations, cam)
        {
        }

        protected override void SetupCameras(CustomCamera cam)
        {
            throw new System.NotImplementedException();
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
            Ctx.OrbitId = 8;
        }

        protected override List<string> SetSourceGroundstations()
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
    }
}