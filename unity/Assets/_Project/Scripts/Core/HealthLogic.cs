using UnityEngine;

namespace SushiSurvival.Core
{
    public static class HealthLogic
    {
        public static float ApplyDamage(float currentHealth, float damage)
        {
            float safeDamage = Mathf.Max(0f, damage);
            return Mathf.Max(0f, currentHealth - safeDamage);
        }

        public static bool IsDead(float currentHealth) => currentHealth <= 0f;
    }
}
