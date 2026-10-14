using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 설명창 연출의 값 계산. 시간(초)만 받아 0~1 진행도와 배율·투명도·위치 오프셋을 돌려준다.
    /// 실제로 오브젝트에 적용하는 일은 AugmentGuidePanel이 한다.
    /// </summary>
    public static class GuideAnimLogic
    {
        public const float OpenDuration = 0.2f;
        public const float CloseDuration = 0.12f;
        public const float TabDuration = 0.15f;
        public const float RowDuration = 0.15f;
        public const float RowStagger = 0.03f;
        /// <summary>줄이 차례로 나타나는 지연의 상한 — 항목이 많아도 전체 연출이 이 안에서 끝난다.</summary>
        public const float MaxRowDelay = 0.15f;

        public const float OpenStartScale = 0.9f;
        public const float CloseEndScale = 0.95f;
        public const float TabRiseDistance = 8f;
        public const float IconBounceAmount = 0.15f;

        /// <summary>경과 시간을 0~1로. 지속 시간이 0 이하면 이미 끝난 것으로 본다.</summary>
        public static float Progress(float elapsed, float duration)
        {
            if (duration <= 0f) return 1f;
            return Mathf.Clamp01(elapsed / duration);
        }

        /// <summary>처음엔 빠르고 끝에서 느려지는 완만한 곡선.</summary>
        public static float EaseOut(float t)
        {
            float u = 1f - Mathf.Clamp01(t);
            return 1f - u * u;
        }

        public static float OpenScale(float t) => Mathf.Lerp(OpenStartScale, 1f, EaseOut(t));

        public static float OpenAlpha(float t) => EaseOut(t);

        public static float CloseScale(float t) => Mathf.Lerp(1f, CloseEndScale, Mathf.Clamp01(t));

        public static float CloseAlpha(float t) => 1f - Mathf.Clamp01(t);

        /// <summary>탭 내용이 아래쪽에서 올라오는 거리 — 시작 8, 끝 0.</summary>
        public static float TabOffset(float t) => TabRiseDistance * (1f - EaseOut(t));

        public static float TabAlpha(float t) => EaseOut(t);

        /// <summary>index번째 줄이 나타나기 시작하는 시각(초).</summary>
        public static float RowDelay(int index) => Mathf.Min(Mathf.Max(0, index) * RowStagger, MaxRowDelay);

        /// <summary>탭을 연 지 elapsed초일 때 index번째 줄의 진행도.</summary>
        public static float RowProgress(float elapsed, int index) =>
            Progress(elapsed - RowDelay(index), RowDuration);

        /// <summary>한 번 살짝 튀는 배율: 1 → 1.15 → 1.</summary>
        public static float Bounce(float t) => 1f + IconBounceAmount * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));

        /// <summary>전체 탭 연출이 끝나는 시각 — 가장 늦게 시작하는 줄이 끝날 때.</summary>
        public static float TabTotalDuration => MaxRowDelay + RowDuration;

        /// <summary>펫 그림 프레임. 프레임이 없으면 -1, 있으면 시간에 따라 0..count-1을 반복한다.</summary>
        public static int FrameIndex(float time, float framesPerSecond, int frameCount)
        {
            if (frameCount <= 0) return -1;
            if (frameCount == 1 || framesPerSecond <= 0f) return 0;
            int step = Mathf.FloorToInt(Mathf.Max(0f, time) * framesPerSecond);
            return step % frameCount;
        }
    }
}
