namespace SushiSurvival.Enemies.Boss
{
    public enum BossPatternType
    {
        /// <summary>빨간 구슬 — 메테오 낙하 광역기.</summary>
        Meteor,
        /// <summary>초록 구슬 — 잡몹 소환.</summary>
        Summon
    }

    /// <summary>
    /// 패턴을 번갈아 고른다. 무작위로 뽑으면 소환이 연달아 나와 화면이 잡몹으로
    /// 덮이거나, 메테오만 연달아 나와 단조로워진다.
    /// </summary>
    public static class BossPatternScheduler
    {
        public static BossPatternType SelectNext(BossPatternType previous)
            => previous == BossPatternType.Meteor ? BossPatternType.Summon : BossPatternType.Meteor;
    }
}
