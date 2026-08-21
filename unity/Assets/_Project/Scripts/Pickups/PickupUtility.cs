using UnityEngine;

namespace SushiSurvival.Pickups
{
    public static class PickupUtility
    {
        public static bool IsWithinPickupRadius(Vector2 a, Vector2 b, float radius)
            => (a - b).sqrMagnitude <= radius * radius;
    }
}
