/* C138 Final Year Project 2023-2024 */

namespace Attack.Cases
{
    public enum CaseChoice
    {
        SimpleDemo, // Small Example Case

        // Geographic
        Landlocked, // Intra-continental
        Coastal, // Continental coast
        Insular, // Islands

        // Latitudinal
        Polar, // High link density
        Equatorial, // Low link density

        // Orbital
        TransOrbital, // Across orbits
        IntraOrbital // Within the same orbit
    };
}