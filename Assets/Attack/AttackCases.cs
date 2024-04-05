using UnityEditor;

namespace Attack
{
    public static class AttackCases
    {
        public static void getTargetCoordinates(AttackChoice attack_choice, out float target_lat, out float target_lon)
        {
            target_lat = 0f;
            target_lon = 0f;
            switch (attack_choice)
            {
                // TODO: add default case. (the Miami attack)
                case AttackChoice.Demo:
                    // Simple example demo between Miami & Chicago
                    target_lat = 32f;
                    target_lon = 82f;
                    break;
                case AttackChoice.TranscontinentalUS:
                    // Minneapolis, USA
                    target_lat = 44.9778f;
                    target_lon = 93.2650f;
                    break;
                case AttackChoice.CoastalUS:
                    // San Francisco, USA
                    target_lat = 37.7749f;
                    target_lon = 122.4194f;
                    break;
                case AttackChoice.Polar:
                    // Lake River, Canada
                    target_lat = 54f;
                    target_lon = 82f;
                    break;
                case AttackChoice.Equatorial:
                    // Quito, Equador
                    target_lat = 0f;
                    target_lon = 78f;
                    break;
                case AttackChoice.IntraOrbital:
                    // TODO: Create a set of groundstations that would force intra orbital routing.
                    target_lat = 0f;
                    target_lon = 0f;
                    break;
                case AttackChoice.TransOrbital:
                    // TODO: Create a set of groundstations that would force trans orbital routing.
                    target_lat = 0f;
                    target_lon = 0f;
                    break;
            }
        }
    }
}