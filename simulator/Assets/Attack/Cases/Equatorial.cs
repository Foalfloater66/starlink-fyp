using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{
    public class Equatorial : BaseCase
    {
        public Equatorial(CityCreator cityCreator, Direction targetLinkDirection,
            GroundstationCollection groundstations, CustomCamera cam) : base(cityCreator, targetLinkDirection,
            groundstations, cam)
        {
        }

        protected override void SetupCameras(CustomCamera cam)
        {
            cam.ViewAmericanEquator();
        }

        protected override void CreateCities()
        {
            if (new HashSet<Direction> { Direction.North, Direction.South }.Contains(Ctx.Direction))
            {
                Cities.USACities();
                Cities.SACities();
                Cities.CarribbeanCities();
            }

            if (Ctx.Direction == Direction.East)
            {
                // Cities.ASEANCities();
                // Cities.OceaniaCities();
                Cities.AFCities();
                Cities.WPacific();
                Cities.SACities();
                Cities.EUCities();
                return;
            }

            if (new HashSet<Direction> { Direction.West, Direction.Any }.Contains(Ctx.Direction))
            {
                Cities.ASEANCities();
                Cities.OceaniaCities();
                Cities.AFCities();
                Cities.WPacific();
                Cities.SACities();
            }
        }

        protected override void CreateTargetCoordinates()
        {
            Ctx.Latitude = 10.41f;
            Ctx.Longitude = 67.39f;
        }

        protected override List<string> SetSourceGroundstations()
        {
            switch (Ctx.Direction)
            {
                case Direction.South:
                    return new List<string>
                    {
                        // Northern countries (mostly NA)
                        "New York",
                        "Miami",
                        // "Caracas",
                        "Guadeloupe",
                        "Maracaibo",
                        "Boston",
                        "Chicago"
                    };
                case Direction.North:
                    return new List<string>
                    {
                        // Southern countries (SA)
                        "Guayaquil",
                        // "Lima",
                        "Caracas",
                        "Medellin",
                        "Quito",
                        "Cali",
                        "Maracaibo",
                    };
                case Direction.East:
                    return new List<string>
                    {
                        "Bogota",
                        "Guayaquil",
                        "Cali",
                        "Medellin",
                        "Quito",
                        "Maracaibo",
                    };

                case Direction.West:
                    return new List<string>
                    {
                        // African cities
                        "Kinshasa",
                        "Luanda",
                        "Cairo",
                        "Dar es Salaam",
                        "Nairobi",
                        "Abidjan",
                    };
                case Direction.Any:
                default:
                    return new List<string>
                    {
                        // South
                        "Guayaquil",
                        "Lima",
                        "Quito",
                        // North
                        "New York",
                        "Miami",
                        "Caracas"
                    };
            }
        }
    }
}