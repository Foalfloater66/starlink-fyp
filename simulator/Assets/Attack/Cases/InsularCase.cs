using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{
    public class InsularCase : BaseCase
    {
        public InsularCase(CityCreator cityCreator, Direction targetLinkDirection,
            GroundstationCollection groundstations, CustomCamera cam) : base(cityCreator, targetLinkDirection,
            groundstations, cam)
        {
        }

        protected override void SetupCameras(CustomCamera cam)
        {
            cam.ViewInsular();
        }

        protected override void CreateCities()
        {
            // TODO: not finished.
            Cities.NACities();
            Cities.NonASEANCities();
            Cities.ASEANCities();
            Cities.WPacific();
            Cities.PacificCities();
            Cities.SACities();
            Cities.AFCities();
            Cities.OceaniaCities();
        }

        protected override void CreateTargetCoordinates()
        {
            // Hawaii, USA
            Ctx.Latitude = 19.89f;
            Ctx.Longitude = 155.66f;
        }

        protected override List<string> SetSourceGroundstations()
        {
            switch (Ctx.Direction)
            {
                case Direction.North:
                    return new List<string>()
                    {
                        "Christchurch",
                        "Sydney",
                        "Brisbane",
                        "Melbourne",
                        "Wellington",
                        "Port Moresby"
                    };
                case Direction.South:
                    return new List<string>()
                    {
                        "Seattle",
                        "Port Hardy",
                        "San Jose",
                        "Winnipeg",
                        "Edmonton",
                        "New York"
                    };

                case Direction.East:
                    return new List<string>()
                    {
                        // North and South Asian countries.
                        "Shikotan",
                        "Severo-Kurilsky",
                        "Adak",
                        "Delhi",
                        "Lahore",
                        "Chongqing"
                    };
                case Direction.West:
                    return new List<string>()
                    {
                        "Johannesburg",
                        "Ekurhuleni",
                        "Durban",
                        "Dar es Salaam",
                        "Hawaii",
                        "Belo Horizonte"
                    };

                case Direction.Any:
                default:
                    return new List<string>()
                    {
                        "Christchurch",
                        "Johannesburg",
                        "Severo-Kurilsky",
                        "Seattle",
                        "Port Hardy",
                        "Brisbaine"
                    };
            }
        }
    }
}