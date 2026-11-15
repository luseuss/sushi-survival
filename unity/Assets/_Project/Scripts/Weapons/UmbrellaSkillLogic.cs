using UnityEngine;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 회전 우산을 마우스 클릭으로 발동하는 스킬의 순수 계산부 — 발동 가능 여부, 미리 누른 클릭,
    /// 쿨타임 막대 채움 비율, 튀어나오는 크기 곡선. 입력과 화면 적용은 RotatingUmbrellaWeapon이 한다.
    /// </summary>
    public static class UmbrellaSkillLogic
    {
        /// <summary>쿨타임이 다 돌았으면(0 이하) 발동할 수 있다.</summary>
        public static bool IsReady(float cooldownRemaining) => cooldownRemaining <= 0f;

        /// <summary>
        /// 쿨타임이 끝나기 직전(window초 이내)에 누른 클릭은 기억해 두었다가 준비되는 순간 발동한다.
        /// 이미 준비됐거나 쿨타임이 한참 남았으면 기억하지 않는다.
        /// </summary>
        public static bool ShouldBuffer(float cooldownRemaining, float window)
            => cooldownRemaining > 0f && cooldownRemaining <= window;

        /// <summary>시간이 멈춘 상태(레벨업·일시정지 창)이거나 화면 위 버튼을 누르는 중이면 클릭을 받지 않는다.</summary>
        public static bool ClickAccepted(float timeScale, bool pointerOverUi) => timeScale > 0f && !pointerOverUi;

        /// <summary>쿨타임 막대가 찬 정도(0~1). 지속 시간이 0 이하면 이미 찬 것으로 본다.</summary>
        public static float FillRatio(float cooldownRemaining, float cooldownDuration)
        {
            if (cooldownDuration <= 0f) return 1f;
            return Mathf.Clamp01(1f - cooldownRemaining / cooldownDuration);
        }

        /// <summary>
        /// 우산이 튀어나올 때의 크기 배율. 0에서 시작해 살짝 커졌다가(오버슛) 1에 자리 잡는다(easeOutBack).
        /// t는 0~1 진행도.
        /// </summary>
        public static float PopScale(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
