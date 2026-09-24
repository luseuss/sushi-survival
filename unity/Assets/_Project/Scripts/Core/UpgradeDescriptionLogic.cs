using System.Collections.Generic;
using System.Globalization;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 레벨업 카드 아래에 적는 한 줄 설명을 만든다. 증강은 스탯과 한 번에 오르는 값으로, 무기 강화는
    /// 현재 레벨과 다음 레벨의 수치 차이로 자동 생성해서, 밸런스 수치를 고쳐도 설명이 어긋나지 않게 한다.
    /// </summary>
    public static class UpgradeDescriptionLogic
    {
        // 카드에 들어가는 줄 수. 넘치면 우선순위가 낮은 항목부터 뺀다.
        private const int MaxWeaponLines = 3;

        /// <summary>증강 한 번 고를 때의 효과 문장. 배율 스탯은 %로, 고정값 스탯은 숫자로 보여준다.</summary>
        public static string DescribeAugment(StatType stat, float valuePerPick)
        {
            switch (stat)
            {
                case StatType.AttackDamage: return $"공격력 +{Percent(valuePerPick)}%";
                case StatType.AttackSpeed: return $"공격 속도 +{Percent(valuePerPick)}%";
                case StatType.AttackRange: return $"공격 범위 +{Percent(valuePerPick)}%";
                case StatType.MaxHealth: return $"최대 체력 +{Number(valuePerPick)}";
                case StatType.Armor: return $"받는 피해 -{Percent(valuePerPick)}%";
                case StatType.Regen: return $"초당 체력 {Number(valuePerPick)} 회복";
                case StatType.MoveSpeed: return $"이동 속도 +{Percent(valuePerPick)}%";
                case StatType.MagnetRange: return $"자석 범위 +{Percent(valuePerPick)}%";
                case StatType.ExpGain: return $"경험치 획득량 +{Percent(valuePerPick)}%";
                case StatType.Revive: return $"쓰러져도 부활 +{Number(valuePerPick)}회";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// 무기 강화 카드 설명. 바뀌는 수치만 "현재 → 다음"으로 적는다. 데미지, 쿨타임 순으로 먼저 보여주고
        /// 범위·각도·관통은 그 뒤에 둔다. 줄이 넘치면 뒤쪽을 뺀다.
        /// </summary>
        public static string DescribeWeaponUpgrade(WeaponLevelStats current, WeaponLevelStats next)
        {
            var lines = new List<string>();

            if (Changed(current.damage, next.damage))
                lines.Add($"공격력 {Number(current.damage)} → {Number(next.damage)}");

            if (Changed(current.cooldown, next.cooldown))
                lines.Add($"쿨타임 {Number(current.cooldown)} → {Number(next.cooldown)}초");

            if (Changed(current.range, next.range) && next.range > 0f)
                lines.Add($"범위 {Number(current.range)} → {Number(next.range)}");

            if (Changed(current.angleDegrees, next.angleDegrees) && next.angleDegrees > 0f)
                lines.Add($"각도 {Number(current.angleDegrees)}° → {Number(next.angleDegrees)}°");

            if (current.pierceCount != next.pierceCount)
                lines.Add($"관통 {current.pierceCount} → {next.pierceCount}");

            if (lines.Count > MaxWeaponLines)
                lines.RemoveRange(MaxWeaponLines, lines.Count - MaxWeaponLines);

            return string.Join("\n", lines);
        }

        private static bool Changed(float a, float b) => System.Math.Abs(a - b) > 0.0001f;

        // 0.2 → "20", 0.06 → "6". 부동소수 오차(0.1f*100=10.000001)가 "10.000001"로 보이지 않게 반올림한다.
        private static string Percent(float fraction)
            => ((int)System.Math.Round(fraction * 100f)).ToString(CultureInfo.InvariantCulture);

        // 정수면 "30", 소수면 "0.5"·"1.15"처럼 불필요한 0 없이 보여준다.
        private static string Number(float value)
            => System.Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
    }
}
