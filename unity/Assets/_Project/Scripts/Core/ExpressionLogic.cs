using UnityEngine;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>한 순간의 표정 연출 값 — 위치 오프셋(픽셀), 배율, 회전(도), 색을 입히는 정도와 색.</summary>
    public struct ExpressionPose
    {
        public Vector2 Offset;
        public float Scale;
        public float RotationDegrees;
        public float TintStrength;
        public Color Tint;

        public static ExpressionPose Identity => new ExpressionPose { Scale = 1f, Tint = Color.white };
    }

    /// <summary>
    /// 표정별 연출 계산. 시간(초)을 받아 그 순간의 자세와 말풍선 값을 돌려주는 순수 함수라 적용은 ExpressionEffect가 한다.
    /// 연출은 한 번 재생하고 끝난다 — 끝난 뒤에는 항상 Identity다.
    /// </summary>
    public static class ExpressionLogic
    {
        public const float BubbleDuration = 1.4f;
        private const float BubblePopTime = 0.15f;
        private const float BubbleFadeTime = 0.3f;

        private static readonly Color AngryTint = new Color(1f, 0.55f, 0.5f, 1f);
        private static readonly Color SadTint = new Color(0.62f, 0.66f, 0.8f, 1f);
        private static readonly Color EmbarrassedTint = new Color(1f, 0.75f, 0.8f, 1f);

        /// <summary>연출이 끝나는 시각(초). 평상시는 0.</summary>
        public static float Duration(ExpressionType expression)
        {
            switch (expression)
            {
                case ExpressionType.Happy: return 0.8f;
                case ExpressionType.Surprised: return 0.5f;
                case ExpressionType.Angry: return 1.0f;
                case ExpressionType.Sad: return 1.0f;
                case ExpressionType.Embarrassed: return 0.9f;
                default: return 0f;
            }
        }

        /// <summary>말풍선 글자. 평상시는 빈 문자열(말풍선 없음).</summary>
        public static string Glyph(ExpressionType expression)
        {
            switch (expression)
            {
                case ExpressionType.Happy: return "♪";
                case ExpressionType.Surprised: return "!";
                case ExpressionType.Angry: return "#";
                case ExpressionType.Sad: return "…";
                case ExpressionType.Embarrassed: return ";;";
                default: return string.Empty;
            }
        }

        public static Color GlyphColor(ExpressionType expression)
        {
            switch (expression)
            {
                case ExpressionType.Happy: return new Color(1f, 0.85f, 0.35f, 1f);
                case ExpressionType.Surprised: return new Color(1f, 0.95f, 0.5f, 1f);
                case ExpressionType.Angry: return new Color(1f, 0.3f, 0.3f, 1f);
                case ExpressionType.Sad: return new Color(0.7f, 0.78f, 1f, 1f);
                case ExpressionType.Embarrassed: return new Color(1f, 0.6f, 0.7f, 1f);
                default: return Color.white;
            }
        }

        /// <summary>표정을 짓기 시작한 지 t초 뒤의 자세.</summary>
        public static ExpressionPose Evaluate(ExpressionType expression, float t)
        {
            float duration = Duration(expression);
            if (duration <= 0f || t < 0f || t >= duration) return ExpressionPose.Identity;

            float progress = t / duration;
            float fade = 1f - progress;
            ExpressionPose pose = ExpressionPose.Identity;

            switch (expression)
            {
                case ExpressionType.Happy:
                    // 두 번 통통 뛰며 점점 작아진다.
                    pose.Offset = new Vector2(0f, 18f * Mathf.Abs(Mathf.Sin(t * Mathf.PI / 0.4f)) * fade);
                    break;

                case ExpressionType.Surprised:
                    // 위로 튀어 오르고 크기가 살짝 커졌다 돌아온다.
                    pose.Offset = new Vector2(0f, t < 0.3f ? 40f * Mathf.Sin(Mathf.PI * (t / 0.3f)) : 0f);
                    pose.Scale = 1f + 0.12f * Mathf.Sin(Mathf.PI * progress);
                    break;

                case ExpressionType.Angry:
                    // 빠르게 떨며 붉은 기운이 돈다.
                    pose.Offset = new Vector2(6f * Mathf.Sin(t * 70f) * fade, 0f);
                    pose.Tint = AngryTint;
                    pose.TintStrength = 0.5f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, progress * 1.6f));
                    break;

                case ExpressionType.Sad:
                    // 아래로 처지며 어두워졌다 돌아온다.
                    pose.Offset = new Vector2(0f, -14f * Mathf.Sin(Mathf.PI * progress));
                    pose.Tint = SadTint;
                    pose.TintStrength = 0.55f * Mathf.Sin(Mathf.PI * progress);
                    break;

                case ExpressionType.Embarrassed:
                    // 좌우로 흔들리며 볼이 붉어진다.
                    pose.Offset = new Vector2(10f * Mathf.Sin(t * 30f) * fade, 0f);
                    pose.RotationDegrees = 3f * Mathf.Sin(t * 30f) * fade;
                    pose.Tint = EmbarrassedTint;
                    pose.TintStrength = 0.4f * Mathf.Sin(Mathf.PI * progress);
                    break;
            }

            return pose;
        }

        /// <summary>말풍선 배율: 0.15초 동안 0 → 1.2 → 1로 톡 튀어나오고 이후 1.</summary>
        public static float BubbleScale(float t)
        {
            if (t < 0f) return 0f;
            if (t >= BubblePopTime) return 1f;

            float p = t / BubblePopTime;
            return p < 0.6f ? Mathf.Lerp(0f, 1.2f, p / 0.6f) : Mathf.Lerp(1.2f, 1f, (p - 0.6f) / 0.4f);
        }

        /// <summary>말풍선 투명도: 끝나기 0.3초 전부터 사라진다.</summary>
        public static float BubbleAlpha(float t)
        {
            if (t < 0f) return 0f;
            if (t >= BubbleDuration) return 0f;

            float fadeStart = BubbleDuration - BubbleFadeTime;
            return t < fadeStart ? 1f : 1f - (t - fadeStart) / BubbleFadeTime;
        }
    }
}
