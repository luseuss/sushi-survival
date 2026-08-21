using UnityEngine;

namespace SushiSurvival.Weapons
{
    public static class CooldownLogic
    {
        /// <summary>
        /// 공격속도 배율을 쿨타임에 적용한다. 배율이 클수록 쿨타임이 짧아지며,
        /// 무기별 최소 쿨타임 절대값이 하한을 잡는다(무한 연사 방지).
        /// </summary>
        public static float ApplyAttackSpeed(float baseCooldown, float attackSpeedMultiplier, float minCooldown)
        {
            if (attackSpeedMultiplier <= 0f)
                return baseCooldown;

            return Mathf.Max(baseCooldown / attackSpeedMultiplier, minCooldown);
        }
    }
}
