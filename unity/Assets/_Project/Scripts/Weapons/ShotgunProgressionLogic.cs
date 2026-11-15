namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 카마리온 무기 성장 규칙 — 간장 소총이 정해진 레벨에서 와사비 샷건 스킬(클릭 발동)을 얻는 흐름의 순수 계산부.
    /// 아델린의 <see cref="UmbrellaProgressionLogic"/>과 같은 방식이다.
    /// </summary>
    public static class ShotgunProgressionLogic
    {
        /// <summary>이 레벨이 되면 와사비 샷건 스킬을 얻을지.</summary>
        public static bool ShouldEvolve(int level, int evolveLevel) => level >= evolveLevel;

        /// <summary>
        /// 소총 강화 카드 문구. 다음 레벨에서 와사비 샷건 스킬을 얻으면 수치 비교 대신 스킬 안내를 보여준다
        /// (산탄 한 발 피해는 소총 피해의 비율이라 소총 수치끼리 비교하면 오해를 부른다).
        /// </summary>
        public static string DescribeRifleUpgrade(string statText, bool evolvesNext, int pelletCount)
        {
            if (!evolvesNext) return statText;

            return $"와사비 샷건 스킬 획득!\n클릭하면 산탄 {pelletCount}발을 부채꼴로 쏜다";
        }
    }
}
