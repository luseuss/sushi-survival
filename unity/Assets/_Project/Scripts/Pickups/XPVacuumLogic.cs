using UnityEngine;

namespace SushiSurvival.Pickups
{
    /// <summary>
    /// 경험치 흡입 아이템의 순수 계산부 — 떨어질지 정하는 규칙과, 젬이 플레이어에게 빨려 오는 속도.
    /// </summary>
    public static class XPVacuumLogic
    {
        /// <summary>
        /// 이번 처치에서 아이템이 떨어지는지. roll은 0 이상 1 미만의 난수다. 확률 0은 절대 안 떨어지고,
        /// 1은 항상 떨어진다.
        /// </summary>
        public static bool ShouldDrop(float chance, float roll)
            => chance > 0f && roll < chance;

        /// <summary>필드에 이미 있는 아이템 수가 상한보다 적을 때만 새로 만든다. 상한이 0 이하면 제한 없음.</summary>
        public static bool CanSpawn(int activeCount, int maxActive)
            => maxActive <= 0 || activeCount < maxActive;

        /// <summary>
        /// 빨려 오기 시작한 뒤 elapsed초가 지났을 때의 속도. startSpeed에서 시작해 초당 acceleration씩
        /// 빨라지다 maxSpeed에서 멈춘다. 멀리 있는 젬도 곧 따라붙어 한꺼번에 도착하는 느낌을 만든다.
        /// </summary>
        public static float AttractSpeed(float elapsed, float startSpeed, float acceleration, float maxSpeed)
            => Mathf.Min(maxSpeed, startSpeed + acceleration * Mathf.Max(0f, elapsed));
    }
}
