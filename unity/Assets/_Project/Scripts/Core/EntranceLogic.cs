using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 카드 여러 장이 시차를 두고 차례로 등장하는 연출의 시간 계산. 등장이 끝난 뒤 잠깐 더 입력을 막아
    /// (holdAfter) 장면 전환 직후에 이어지는 연타가 카드를 눌러버리지 않게 한다.
    /// </summary>
    public static class EntranceLogic
    {
        /// <summary>index번째 카드의 등장 진행도(0~1). 시차만큼 늦게 시작해서 duration 동안 진행한다.</summary>
        public static float CardProgress(float elapsed, int index, float stagger, float duration)
        {
            if (duration <= 0f) return 1f;

            float start = Mathf.Max(0, index) * Mathf.Max(0f, stagger);
            return Mathf.Clamp01((elapsed - start) / duration);
        }

        /// <summary>마지막 카드의 등장이 끝나는 시각.</summary>
        public static float AnimationDuration(int cardCount, float stagger, float duration)
        {
            if (cardCount <= 0) return 0f;

            return (cardCount - 1) * Mathf.Max(0f, stagger) + Mathf.Max(0f, duration);
        }

        /// <summary>입력이 다시 열리는 시각 = 등장이 끝난 시각 + 여유 시간.</summary>
        public static float UnlockTime(int cardCount, float stagger, float duration, float holdAfter)
        {
            return AnimationDuration(cardCount, stagger, duration) + Mathf.Max(0f, holdAfter);
        }

        public static bool IsUnlocked(float elapsed, float unlockTime) => elapsed >= unlockTime;

        /// <summary>처음엔 빠르게 다가오고 끝에서 부드럽게 멈춘다.</summary>
        public static float EaseOutCubic(float t)
        {
            float inv = 1f - Mathf.Clamp01(t);
            return 1f - inv * inv * inv;
        }
    }
}
