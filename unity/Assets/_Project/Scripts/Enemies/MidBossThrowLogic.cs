namespace SushiSurvival.Enemies
{
    /// <summary>중형몹이 롤 몬스터를 던지는 패턴의 순수 계산부.</summary>
    public static class MidBossThrowLogic
    {
        /// <summary>체력이 최대 체력 대비 threshold 비율 이하로 내려갔는지. 최대 체력이 0이면 발동하지 않는다.</summary>
        public static bool HealthBelowThreshold(float currentHealth, float maxHealth, float threshold)
            => maxHealth > 0f && currentHealth / maxHealth <= threshold;
    }
}
