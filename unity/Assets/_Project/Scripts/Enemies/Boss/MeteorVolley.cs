using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>
    /// 메테오 연발의 발사 시각을 계산한다. 각 발이 자기 발사 시점의 플레이어
    /// 위치를 잡으므로, 간격만 정해두면 산개는 플레이어의 움직임이 만들어낸다.
    /// </summary>
    public static class MeteorVolley
    {
        public static IReadOnlyList<float> GetLaunchDelays(int count, float spacing)
        {
            var delays = new List<float>();
            if (count <= 0) return delays;

            // 음수 간격이 들어오면 발사 순서가 뒤집힌다.
            float safeSpacing = Mathf.Max(0f, spacing);

            for (int i = 0; i < count; i++)
                delays.Add(i * safeSpacing);

            return delays;
        }

        /// <summary>마지막 발이 터질 때까지의 시간. 패턴 쿨타임이 이보다 짧으면 겹친다.</summary>
        public static float GetTotalDuration(int count, float spacing, float warningTime)
        {
            if (count <= 0) return 0f;

            return (count - 1) * Mathf.Max(0f, spacing) + Mathf.Max(0f, warningTime);
        }
    }
}
