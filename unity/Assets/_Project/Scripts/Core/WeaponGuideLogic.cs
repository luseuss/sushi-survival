using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>무기 설명창의 한 줄 — 한 레벨 올릴 때 무엇이 바뀌는지.</summary>
    public readonly struct WeaponGuideStep
    {
        public readonly int FromLevel;
        public readonly int ToLevel;
        public readonly string Text;

        public WeaponGuideStep(int fromLevel, int toLevel, string text)
        {
            FromLevel = fromLevel;
            ToLevel = toLevel;
            Text = text;
        }
    }

    /// <summary>
    /// 전용 무기 강화 설명을 레벨업 카드와 같은 규칙으로 만든다. 무기가 정해진 레벨에서 다른 무기로 변하면
    /// (양산 → 우산, 소총 → 샷건) 그 레벨은 수치 비교 대신 변신 안내를 보여주고, 그 뒤 레벨은 변한 무기의
    /// 수치로 비교한다.
    /// </summary>
    public static class WeaponGuideLogic
    {
        /// <param name="weapon">처음 무기의 레벨별 수치. 인덱스 0 = Lv1.</param>
        /// <param name="evolvedWeapon">변신 뒤 무기의 수치. 비우면 변신 뒤에도 같은 수치표를 쓴다(소총 → 샷건).</param>
        /// <param name="evolveFromLevel">이 레벨이 되면 무기가 변한다. 0 이하면 변하지 않는다.</param>
        /// <param name="evolveNote">변신하는 레벨에 보여줄 안내 문구.</param>
        public static List<WeaponGuideStep> BuildSteps(WeaponData weapon, WeaponData evolvedWeapon,
                                                       int evolveFromLevel, string evolveNote, bool evolvedIsUmbrella)
        {
            var steps = new List<WeaponGuideStep>();
            if (weapon == null || weapon.levels == null) return steps;

            for (int to = 2; to <= weapon.levels.Length; to++)
            {
                bool evolvesHere = evolveFromLevel > 0 && to == evolveFromLevel;
                bool evolvedAlready = evolveFromLevel > 0 && to > evolveFromLevel;

                string text;
                if (evolvesHere)
                {
                    text = evolveNote;
                }
                else if (evolvedAlready)
                {
                    WeaponData source = evolvedWeapon != null ? evolvedWeapon : weapon;
                    text = Describe(source, to, evolvedIsUmbrella);
                }
                else
                {
                    text = Describe(weapon, to, false);
                }

                steps.Add(new WeaponGuideStep(to - 1, to, text));
            }

            return steps;
        }

        private static string Describe(WeaponData data, int toLevel, bool isUmbrella)
        {
            if (data.levels == null || data.levels.Length < toLevel) return string.Empty;

            return UpgradeDescriptionLogic.DescribeWeaponUpgrade(
                data.levels[toLevel - 2], data.levels[toLevel - 1], isUmbrella);
        }
    }
}
