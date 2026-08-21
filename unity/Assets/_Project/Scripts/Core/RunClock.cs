using UnityEngine;

namespace SushiSurvival.Core
{
    public static class RunClock
    {
        /// <summary>
        /// 남은 시간을 "4:23" 형식으로. 올림이라 마지막 1초가 화면에 보인다.
        /// </summary>
        public static string FormatRemaining(float elapsed, float duration)
        {
            float remaining = Mathf.Clamp(duration - elapsed, 0f, duration);
            return Format(Mathf.CeilToInt(remaining));
        }

        /// <summary>
        /// 경과 시간을 "3:42" 형식으로. 내림이라 지나간 시간만 표시된다.
        /// </summary>
        public static string FormatElapsed(float seconds)
        {
            float safe = Mathf.Max(0f, seconds);
            return Format(Mathf.FloorToInt(safe));
        }

        private static string Format(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes}:{seconds:00}";
        }
    }
}
