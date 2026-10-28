using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 장면 전환 연출의 값 계산 — 나갈 때(줌인하며 어두워짐)와 들어올 때(어둠이 걷힘)의 진행도·배율·투명도.
    /// 시간만 받는 순수 함수라 적용은 SceneTransition이 한다.
    /// </summary>
    public static class SceneTransitionLogic
    {
        /// <summary>나가는 연출 시간(초).</summary>
        public const float OutDuration = 0.5f;

        /// <summary>들어오는 연출 시간(초).</summary>
        public const float InDuration = 0.5f;

        /// <summary>나가는 연출이 끝날 때까지 화면이 커지는 배율.</summary>
        public const float MaxZoom = 1.15f;

        public static float Progress(float elapsed, float duration)
        {
            if (duration <= 0f) return 1f;
            return Mathf.Clamp01(elapsed / duration);
        }

        /// <summary>처음엔 천천히, 갈수록 빨라지는(빨려 들어가는) 곡선.</summary>
        public static float EaseIn(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        /// <summary>처음엔 빠르고 끝에서 느려지는 곡선.</summary>
        public static float EaseOut(float t)
        {
            float u = 1f - Mathf.Clamp01(t);
            return 1f - u * u;
        }

        /// <summary>나가는 동안 옛 화면의 배율: 1 → MaxZoom.</summary>
        public static float ZoomScale(float t) => Mathf.Lerp(1f, MaxZoom, EaseIn(t));

        /// <summary>나가는 동안 화면을 덮는 검은색의 투명도: 0 → 1.</summary>
        public static float OutAlpha(float t) => EaseIn(t);

        /// <summary>들어오는 동안 검은색의 투명도: 1 → 0.</summary>
        public static float InAlpha(float t) => 1f - EaseOut(t);
    }
}
