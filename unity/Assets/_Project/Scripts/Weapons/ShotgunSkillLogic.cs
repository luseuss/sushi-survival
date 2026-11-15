using UnityEngine;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 카마리온의 와사비 샷건 스킬의 순수 계산부. 쿨타임·준비·미리 누른 클릭 판정은 아델린의 우산과 같은
    /// <see cref="UmbrellaSkillLogic"/>을 쓰고, 여기서는 "켜져 있는 동안"에만 필요한 값을 계산한다.
    /// </summary>
    public static class ShotgunSkillLogic
    {
        /// <summary>스킬이 켜져 있는 동안 막대에 보일 남은 시간 비율(1 → 0). 지속 시간이 0 이하면 0.</summary>
        public static float ActiveRatio(float activeRemaining, float activeDuration)
        {
            if (activeDuration <= 0f) return 0f;
            return Mathf.Clamp01(activeRemaining / activeDuration);
        }

        /// <summary>
        /// 이번 프레임에 스킬을 시작할지. 스킬을 얻었고, 지금 켜져 있지 않고, 쿨타임이 끝났고,
        /// 클릭(미리 눌러 둔 것 포함)이 있었을 때만 true.
        /// </summary>
        public static bool ShouldStart(bool hasSkill, bool isActive, float cooldownRemaining, bool clickPending)
            => hasSkill && !isActive && UmbrellaSkillLogic.IsReady(cooldownRemaining) && clickPending;
    }
}
