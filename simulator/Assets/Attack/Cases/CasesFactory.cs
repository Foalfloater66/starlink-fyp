/* C138 Final Year Project 2023-2024 */

using Orbits;
using Utilities;

namespace Attack.Cases
{
    public static class CasesFactory
    {
        public static BaseAttribute GetCase(CaseChoice choice, CityCreator cities, Direction targetLinkDirection,
            GroundstationCollection groundstations, CustomCamera cam)
        {
            switch (choice)
            {
                case CaseChoice.Coastal:
                    return new Coastal(cities, targetLinkDirection, groundstations, cam);
                case CaseChoice.Landlocked:
                    return new Landlocked(cities, targetLinkDirection, groundstations, cam);
                case CaseChoice.Insular:
                    return new Insular(cities, targetLinkDirection, groundstations, cam);
                case CaseChoice.Polar:
                    return new Polar(cities, targetLinkDirection, groundstations, cam);
                case CaseChoice.Equatorial:
                    return new Equatorial(cities, targetLinkDirection, groundstations, cam);
                case CaseChoice.IntraOrbital:
                    return new IntraOrbital(cities, targetLinkDirection, groundstations, cam);
                case CaseChoice.TransOrbital:
                    return new TransOrbital(cities, targetLinkDirection, groundstations, cam);
                case CaseChoice.SimpleDemo:
                default:
                    return new SimpleDemo(cities, targetLinkDirection, groundstations, cam);
            }
        }
    }
}