using System.Globalization;
using SushiSurvival.Data;

namespace SushiSurvival.Companions
{
    public static class FairyDescriptionLogic
    {
        /// <summary>소환 카드 설명: 역할 한 줄 + 피해·쿨타임·사거리 한 줄(역할이 비면 수치 줄만).</summary>
        public static string DescribeSummon(string roleLine, WeaponLevelStats level1)
        {
            string stats = $"피해 {Number(level1.damage)} · 쿨타임 {Number(level1.cooldown)}초 · 사거리 {Number(level1.range)}";

            return string.IsNullOrEmpty(roleLine) ? stats : roleLine + "\n" + stats;
        }

        // 정수면 "10", 소수면 "1.8"처럼 불필요한 0 없이 보여준다.
        private static string Number(float value)
            => System.Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
    }
}
