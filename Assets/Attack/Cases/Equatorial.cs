using System.Collections.Generic;
using Orbits;
using Utilities;

namespace Attack.Cases
{
    public class Equatorial : BaseCase
    {
        public Equatorial(CityCreator cityCreator, Direction targetLinkDirection, GroundstationCollection groundstations, CustomCamera cam) : base(cityCreator, targetLinkDirection, groundstations, cam)
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
            }
            if (new HashSet<Direction> { Direction.East, Direction.West, Direction.Any}.Contains(Ctx.Direction))
            {
                Cities.ASEANCities();
                Cities.OceaniaCities();
                Cities.AFCities();
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
                        "Caracas",
                        "Maracaibo",
                        "Boston",
                        "Chicago",
                    };
                case Direction.North:
                    return new List<string>
                    {
                        // Southern countries (SA)
                        "Guayaquil",
                        "Lima",
                        "Medellin",
                        "Quito",
                        "Cali"
                    };
                case Direction.East:
                    return new List<string>
                    {
                        "Nishino, Kagoshima",
                        "Hengchung",
                        "Caracas",
                        "Maracaibo",
                        "Medellin",
                        "Quito",
                    };
                    
                case Direction.West:
                    return new List<string>
                    {
                        // Northern countries (mostly NA)
                        "New York",
                        // "Miami",
                        "Caracas",
                        "Maracaibo",
                        // "Boston",
                        // "Chicago",
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
                        "Caracas",
                    };
            }

        }
    }
}