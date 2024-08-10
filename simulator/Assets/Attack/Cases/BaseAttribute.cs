/* C138 Final Year Project 2023-2024 */

using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{
    public abstract class BaseAttribute
    {
        protected AttackerParams Ctx;
        protected readonly CityCreator Cities;

        protected BaseAttribute(CityCreator cityCreator, Direction targetLinkDirection,
            GroundstationCollection groundstations, CustomCamera cam)
        {
            Ctx = AttackerParams.Default;
            Ctx.Direction = targetLinkDirection;
            Cities = cityCreator;

            SetupCameras(cam);
            CreateCities();
            CreateTargetCoordinates();
            var srcGroundstationNames = SetSourceGroundstations();
            foreach (var name in srcGroundstationNames) Ctx.SrcGroundstations.Add(groundstations[name]);
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