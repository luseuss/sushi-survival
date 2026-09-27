using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>고른 성향들에서 대표 성향을 뽑는다.</summary>
    public static class PlayerTraitLogic
    {
        /// <summary>
        /// 가장 많이 고른 성향. 동수면 picks에서 가장 뒤에 나온(가장 나중에 고른) 성향이 이긴다 —
        /// 질문이 2번뿐이라 1대1 동률이 흔한데, 마지막 답이 이기는 쪽이 자연스럽다. None은 세지 않는다.
        /// </summary>
        public static PlayerTrait Dominant(IReadOnlyList<PlayerTrait> picks)
        {
            if (picks == null || picks.Count == 0) return PlayerTrait.None;

            var counts = new Dictionary<PlayerTrait, int>();
            int max = 0;

            foreach (PlayerTrait pick in picks)
            {
                if (pick == PlayerTrait.None) continue;

                counts.TryGetValue(pick, out int count);
                counts[pick] = ++count;
                if (count > max) max = count;
            }

            if (max == 0) return PlayerTrait.None;

            for (int i = picks.Count - 1; i >= 0; i--)
            {
                if (picks[i] != PlayerTrait.None && counts[picks[i]] == max)
                    return picks[i];
            }

            return PlayerTrait.None;
        }
    }
}
