using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>적을 맞힌 자리에 번쩍이는 스파크의 순수 계산부 — 모양 마스크와 시간에 따른 크기·투명도.</summary>
    public static class HitSparkLogic
    {
        private const float CoreRadius = 0.22f;
        private const float DiagonalLength = 0.55f;
        private const float Sharpness = 8f;

        /// <summary>
        /// 정규화 좌표(-1~1)가 별 모양 안인지. 가운데 작은 원 + 상하좌우로 긴 가시 4개 + 대각선으로 짧은 가시 4개.
        /// 스파크 스프라이트를 픽셀로 그릴 때 쓴다.
        /// </summary>
        public static bool InsideStar(float nx, float ny)
        {
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            if (r > 1f) return false;
            if (r <= CoreRadius) return true;

            float angle = Mathf.Atan2(ny, nx);
            float spike = Mathf.Pow(Mathf.Abs(Mathf.Cos(2f * angle)), Sharpness);
            float diagonal = DiagonalLength * Mathf.Pow(Mathf.Abs(Mathf.Sin(2f * angle)), Sharpness);
            return r <= Mathf.Max(spike, diagonal);
        }

        /// <summary>진행률(0~1)에 따른 배율. 빠르게 커졌다가 느려지며 끝까지 커진다(ease-out).</summary>
        public static float Scale(float progress, float from, float to)
        {
            float t = Mathf.Clamp01(progress);
            float eased = 1f - (1f - t) * (1f - t);
            return Mathf.Lerp(from, to, eased);
        }

        /// <summary>진행률(0~1)에 따른 투명도. 초반에는 또렷하게 유지하다가 끝에서 사라진다.</summary>
        public static float Alpha(float progress)
        {
            float t = Mathf.Clamp01(progress);
            return 1f - t * t;
        }
    }
}
