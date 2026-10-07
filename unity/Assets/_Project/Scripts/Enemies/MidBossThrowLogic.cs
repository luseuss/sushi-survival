namespace SushiSurvival.Enemies
{
    /// <summary>중형몹이 롤 몬스터를 던지는 패턴의 순수 계산부.</summary>
    public static class MidBossThrowLogic
    {
        /// <summary>플레이어가 던질 수 있는 거리 안에 있는지. 너무 붙어 있거나 너무 멀면 던지지 않는다.</summary>
        public static bool InThrowRange(float distance, float minRange, float maxRange)
            => distance >= minRange && distance <= maxRange;
    }
}
