namespace Attack
{
    public enum AttackChoice
    {
        Demo, // Small Example Case
        LandlockedUS, // Intra-continental
        CoastalUS, // Continental coast
        // TODO: add insular example.
        Polar, // High link density
        Equatorial, // Low link density
        TransOrbital, // Across orbits
        IntraOrbital, // Within the same orbit
    };
}