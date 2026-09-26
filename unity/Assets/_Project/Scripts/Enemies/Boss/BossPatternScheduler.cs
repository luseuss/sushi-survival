namespace SushiSurvival.Enemies.Boss
{
    public enum BossPatternType
    {
        /// <summary>빨간 구슬 — 메테오 낙하 광역기.</summary>
        Meteor,
        /// <summary>붉게 번쩍이며 멈춘 뒤 플레이어 쪽으로 곧장 돌진.</summary>
        Charge
    }

    /// <summary>
    /// 다음 무작위 패턴을 고른다. 소환은 체력 임계로 따로 발동하므로 여기엔 없다. 같은 패턴은
    /// 최대 <see cref="MaxConsecutive"/>번까지 연속으로 나올 수 있고, 그 뒤엔 다른 패턴이 나온다 —
    /// 후보가 둘뿐이라 연속을 아예 막으면 순서가 완전히 고정돼 외워서 피할 수 있게 된다.
    /// 1페이즈는 메테오 위주, 2페이즈는 돌진 위주로 가중치를 둔다.
    /// </summary>
    public static class BossPatternScheduler
    {
        public const int MaxConsecutive = 2;

        private static readonly BossPatternType[] All =
        {
            BossPatternType.Meteor, BossPatternType.Charge
        };

        // All와 같은 순서: 메테오, 돌진.
        private static readonly float[] PhaseOneWeights = { 5f, 2f };
        private static readonly float[] PhaseTwoWeights = { 3f, 5f };

        /// <param name="consecutiveCount">previous가 지금까지 연속으로 나온 횟수.</param>
        /// <param name="roll">0 이상 1 미만의 난수. 밖에서 받아 테스트가 결과를 고정할 수 있게 한다.</param>
        public static BossPatternType SelectNext(BossPatternType previous, int consecutiveCount, int phase, float roll)
        {
            float[] weights = phase >= 2 ? PhaseTwoWeights : PhaseOneWeights;
            bool excludePrevious = consecutiveCount >= MaxConsecutive;

            float total = 0f;
            for (int i = 0; i < All.Length; i++)
            {
                if (excludePrevious && All[i] == previous) continue;
                total += weights[i];
            }

            float target = System.Math.Max(0f, System.Math.Min(roll, 0.999999f)) * total;

            float running = 0f;
            BossPatternType last = All[0];
            for (int i = 0; i < All.Length; i++)
            {
                if (excludePrevious && All[i] == previous) continue;

                last = All[i];
                running += weights[i];
                if (target < running) return All[i];
            }

            return last;
        }
    }
}
