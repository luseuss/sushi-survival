using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 데미지 숫자 팝업의 표시 문자열과 시간에 따른 연출 곡선. t는 0(등장)~1(사라짐)의 진행도다.
    /// </summary>
    public static class DamageNumberLogic
    {
        // 등장 직후 이 구간에서 통통 튀며 커지고, 그 뒤 위로 떠오르다 끝에서 사라진다.
        private const float PopEnd = 0.25f;
        private const float FadeStart = 0.6f;
        private const float Overshoot = 1.70158f;

        /// <summary>소수점을 반올림한 정수 문자열. 0.4처럼 작은 값도 0이 아닌 1로 보여준다.</summary>
        public static string Format(float damage)
        {
            return Mathf.Max(1, Mathf.RoundToInt(damage)).ToString();
        }

        /// <summary>등장할 때 1을 살짝 넘겼다가 1로 안착하는 배율.</summary>
        public static float PopScale(float t)
        {
            float p = Mathf.Clamp01(t / PopEnd);
            float u = p - 1f;
            return 1f + (Overshoot + 1f) * u * u * u + Overshoot * u * u;
        }

        /// <summary>끝의 일부 구간에서만 사라진다. 그 전까지는 또렷하게 보인다.</summary>
        public static float Alpha(float t)
        {
            if (t <= FadeStart) return 1f;

            return Mathf.Clamp01(1f - (t - FadeStart) / (1f - FadeStart));
        }

        /// <summary>0~1로 정규화된 상승량. 처음엔 빠르게 솟고 점점 느려진다.</summary>
        public static float Rise(float t)
        {
            float p = Mathf.Clamp01(t);
            float inv = 1f - p;
            return 1f - inv * inv * inv;
        }
    }
}
