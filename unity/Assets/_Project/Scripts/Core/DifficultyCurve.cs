using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 경과 시간을 잡몹 체력 배율로 바꾼다. 잡몹을 낮은 체력으로 시작시켜
    /// 초반에 한 방에 죽게 하고(안 그러면 몹이 쌓인다), 시간이 갈수록 단단해지게
    /// 한다. 중형몹과 보스는 설계된 고정 체력이라 이 배율을 적용하지 않는다.
    /// </summary>
    public static class DifficultyCurve
    {
        private const float SecondsPerMinute = 60f;

        public static float GetMultiplier(float elapsedSeconds, float perMinute, float maxMultiplier)
        {
            float minutes = Mathf.Max(0f, elapsedSeconds) / SecondsPerMinute;
            float growth = Mathf.Max(0f, perMinute) * minutes;

            // 상한을 실수로 0이나 음수로 두면 잡몹 체력이 0이 되어 즉사한다.
            float cap = Mathf.Max(1f, maxMultiplier);

            return Mathf.Clamp(1f + growth, 1f, cap);
        }
    }
}
