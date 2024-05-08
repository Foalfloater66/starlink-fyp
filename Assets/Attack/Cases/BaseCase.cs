using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{



    public abstract class BaseCase
    {
        protected AttackerParams Ctx;
        protected readonly CityCreator Cities;

        protected BaseCase(CityCreator cityCreator, Direction targetLinkDirection,
            GroundstationCollection groundstations, CustomCamera cam)
        {
            Ctx = AttackerParams.Default;
            Ctx.Direction = targetLinkDirection;
            Cities = cityCreator;

            SetupCameras(cam);
            CreateCities();
            CreateTargetCoordinates();
            List<string> srcGroundstationNames = SetSourceGroundstations();
            foreach (string name in srcGroundstationNames) Ctx.SrcGroundstations.Add(groundstations[name]);
        }

        protected abstract void SetupCameras(CustomCamera cam);
        protected abstract void CreateCities();
        protected abstract void CreateTargetCoordinates();
        protected abstract List<string> SetSourceGroundstations();

        public AttackerParams GetParams()
        {
            return Ctx;
        }
    }
}
