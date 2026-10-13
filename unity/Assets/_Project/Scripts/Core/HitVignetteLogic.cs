using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>플레이어가 맞을 때 화면 가장자리가 붉게 번쩍이는 효과의 순수 계산부 — 가장자리 마스크와 시간에 따른 투명도.</summary>
    public static class HitVignetteLogic
    {
        // 가운데에서 이 비율까지는 투명하게 두고, 그 바깥부터 가장자리로 갈수록 붉어진다.
        private const float ClearRadius = 0.45f;

        /// <summary>
        /// 정규화 좌표(-1~1)의 가장자리 세기(0~1). 가운데는 0, 모서리는 1이다.
        /// 텍스처를 한 번 칠할 때 쓴다 — 매 프레임 계산하지 않는다.
        /// </summary>
        public static float EdgeIntensity(float nx, float ny)
        {
            // 모서리가 정확히 1이 되도록 대각선 길이(√2)로 나눈다.
            float r = Mathf.Sqrt(nx * nx + ny * ny) / 1.41421356f;
            float t = Mathf.Clamp01((r - ClearRadius) / (1f - ClearRadius));
            return t * t * (3f - 2f * t);
        }

        /// <summary>진행률(0~1)에 따른 투명도. 맞는 순간 가장 붉고 빠르게 줄다가 천천히 사라진다.</summary>
        public static float Alpha(float progress, float peakAlpha)
        {
            float t = Mathf.Clamp01(progress);
            float remain = 1f - t;
            return peakAlpha * remain * remain;
        }
    }
}
