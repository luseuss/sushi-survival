namespace SushiSurvival.Enemies.Boss
{
    /// <summary>
    /// 체력 비율로 보스 페이즈를 판정한다. 회복 수단이 없으므로 되돌아가는
    /// 경우를 고려하지 않아도 되고, 그래서 단순 임계 비교로 충분하다.
    /// </summary>
    public static class BossPhaseLogic
    {
        public const int PhaseOne = 1;
        public const int PhaseTwo = 2;

        public static int GetPhase(float currentHealth, float maxHealth, float threshold)
        {
            // 데이터가 비어 있을 때 0으로 나누지 않는다.
            if (maxHealth <= 0f) return PhaseTwo;

            return currentHealth / maxHealth < threshold ? PhaseTwo : PhaseOne;
        }
    }
}
