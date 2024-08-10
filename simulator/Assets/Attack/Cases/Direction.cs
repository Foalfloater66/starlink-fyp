/* C138 Final Year Project 2023-2024 */

namespace Attack.Cases
{
    /// <summary>
    /// Defines the position of the source node in relation to the destination link.
    /// Depends on the general orientation of the attack link. If none is specified,
    /// then the position of the source node should not be selectable.
    /// </summary>
    public enum Direction
    {
        East,
        West,
        North,
        South,
        Any // no preference
    }
}