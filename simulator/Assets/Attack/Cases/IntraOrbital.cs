/* C138 Final Year Project 2023-2024 */

using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{
    public class IntraOrbital : BaseAttribute
    {
        public IntraOrbital(CityCreator cityCreator, Direction targetLinkDirection,
            GroundstationCollection groundstations, CustomCamera cam) : base(cityCreator, targetLinkDirection,
            groundstations, cam)
        {
        }

        protected override void SetupCameras(CustomCamera cam)
        {
            switch (Ctx.Direction)
            {
                case Direction.North:
                case Direction.South:
                    cam.ViewAfrica();
                    break;
                case Direction.East:
                case Direction.West:
                    cam.ViewLandlocked();
                    break;
            }
        }

        protected override void CreateCities()
        {
            switch (Ctx.Direction)
            {
                case Direction.North:
                case Direction.South:
                    Cities.AFCities();
                    Cities.EUCities();
                    break;
                case Direction.East:
                case Direction.West:
                default:
                    Cities.NACities();
                    break;
            }
        }

        protected override void CreateTargetCoordinates()
        {
            switch (Ctx.Direction)
            {
                case Direction.North:
                case Direction.South:
                    // East Darfur, Sudan
                    Ctx.Latitude = 12.06f;
                    Ctx.Longitude = -25f;
                    break;
                case Direction.East:
                case Direction.West:
                case Direction.Any:
                default:
                    // Nebraska, USA
                    Ctx.Latitude = 41.49f;
                    Ctx.Longitude = 99.90f;
                    break;
            }

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
                        "Denver"
                    };
                case Direction.West:
                    {
                        return new List<string>
                    {
                        "Toronto",
                        "New York",
                        "Winnipeg",
                        "Edmundston",
                        "Montreal"
                    };
                    }
                case Direction.North:
                    return new List<string>
                    {
                        "Kampala, Uganda",
                        "Dar es Salaam",
                        "Antananarivo, Madagascar",
                        "Addis Ababa",
                        "Nairobi",
                    };
                case Direction.South:
                    return new List<string>
                    {
                        "Casablanca",
                        "Cairo",
                        "Kano",
                        "Algiers, Algeria",
                        "Alexandria"
                    };
                case Direction.Any:
                default:
                    return new List<string>
                    {
                        "Toronto",
                        "New York",
                        "Chicago",
                        "El Paso",
                        "Houston"
                    };
            }
        }
    }
}