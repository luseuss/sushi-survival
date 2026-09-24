namespace SushiSurvival.Enemies.Boss
{
    public enum BossPatternType
    {
        /// <summary>빨간 구슬 — 메테오 낙하 광역기.</summary>
        Meteor,
        /// <summary>초록 구슬 — 잡몹 소환.</summary>
        Summon,
        /// <summary>붉게 번쩍이며 멈춘 뒤 플레이어 쪽으로 곧장 돌진.</summary>
        Charge
    }

    /// <summary>
    /// 다음 패턴을 고른다. 직전과 같은 패턴은 절대 다시 고르지 않는다 — 소환이 연달아 나오면
    /// 화면이 잡몹으로 덮이고, 같은 패턴이 반복되면 단조롭다. 그 안에서는 페이즈별 가중치로
    /// 무작위로 뽑아, 정해진 순서를 외워서 피하지 못하게 하고 페이즈 2에선 돌진을 더 자주 쓴다.
    /// </summary>
    public static class BossPatternScheduler
    {
        private static readonly BossPatternType[] All =
        {
            BossPatternType.Meteor, BossPatternType.Summon, BossPatternType.Charge
        };

        // All와 같은 순서: 메테오, 소환, 돌진.
        private static readonly float[] PhaseOneWeights = { 5f, 3f, 2f };
        private static readonly float[] PhaseTwoWeights = { 3f, 2f, 5f };

        /// <param name="roll">0 이상 1 미만의 난수. 밖에서 받아 테스트가 결과를 고정할 수 있게 한다.</param>
        public static BossPatternType SelectNext(BossPatternType previous, int phase, float roll)
        {
            float[] weights = phase >= 2 ? PhaseTwoWeights : PhaseOneWeights;

            float total = 0f;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i] != previous) total += weights[i];
            }

            float target = System.Math.Max(0f, System.Math.Min(roll, 0.999999f)) * total;

            float running = 0f;
            BossPatternType last = All[0];
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i] == previous) continue;

                last = All[i];
                running += weights[i];
                if (target < running) return All[i];
            }

            return last;
        }
    }
}
