using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 잡몹 지속 스폰이 시간에 따라 빨라지는 정도를 계산한다. 시작값에서 끝값까지 rampSeconds 동안
    /// 선형으로 변하고, 그 뒤엔 끝값을 유지한다. 처음엔 여유롭게 시작해서 보스전 무렵엔 화면이
    /// 가득 차도록 하려는 것이다.
    /// </summary>
    public static class SpawnRampLogic
    {
        private const float MinInterval = 0.05f;

        public static float Progress(float elapsedSeconds, float rampSeconds)
        {
            if (rampSeconds <= 0f) return 1f;

            return Mathf.Clamp01(elapsedSeconds / rampSeconds);
        }

        /// <summary>다음 스폰까지의 대기 시간(초). 0에 가까우면 프레임마다 스폰되므로 하한을 둔다.</summary>
        public static float Interval(float elapsedSeconds, float startInterval, float endInterval, float rampSeconds)
        {
            float t = Progress(elapsedSeconds, rampSeconds);
            return Mathf.Max(MinInterval, Mathf.Lerp(startInterval, endInterval, t));
        }

        /// <summary>한 번에 스폰할 마릿수. 최소 1마리.</summary>
        public static int BatchSize(float elapsedSeconds, int startBatch, int endBatch, float rampSeconds)
        {
            float t = Progress(elapsedSeconds, rampSeconds);
            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(startBatch, endBatch, t)));
        }
    }
}
