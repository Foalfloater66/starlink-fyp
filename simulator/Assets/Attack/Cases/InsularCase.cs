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
            switch (Ctx.Direction)
            {
                case Direction.North:
                case Direction.South:
                    Cities.OceaniaCities();
                    Cities.NACities();
                    Cities.PacificCities();
                    break;
                case Direction.East:
                case Direction.West:
                    Cities.NorthAsiaCities();
                    Cities.PacificCities();
                    Cities.NonASEANCities();
                    Cities.SACities();
                    Cities.WPacific();
                    break;
                case Direction.Any:
                    Cities.OceaniaCities();
                    Cities.NACities();
                    Cities.PacificCities();
                    Cities.NorthAsiaCities();
                    Cities.PacificCities();
                    Cities.NonASEANCities();
                    Cities.SACities();
                    Cities.WPacific();
                    break;
                    
            }
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
                        "Port Moresby",
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
                        "Severo-Kurilsky", // Russia
                        "Vladivostok",
                        "Irkutsk",
                        "Hawaii",
                        "Chongqing"
                    };
                case Direction.West:
                    return new List<string>()
                    {
                        "Hawaii",
                        "Buenos Aires",
                        "Santiago",
                        "Curitiba",
                        "Belo Horizonte",
                        "Ushuaia, Argentina"
                    };

                case Direction.Any:
                default:
                    return new List<string>()
                    {
                        "Christchurch",
                        "Severo-Kurilsky",
                        "Seattle",
                        "Port Hardy",
                        "Brisbaine",
                        "Ushuaia, Argentina"
                    };
            }
        }
    }
}