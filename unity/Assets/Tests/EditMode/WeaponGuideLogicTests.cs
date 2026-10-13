using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class WeaponGuideLogicTests
    {
        private static WeaponData Make(params (float damage, float cooldown)[] levels)
        {
            var data = ScriptableObject.CreateInstance<WeaponData>();
            data.levels = new WeaponLevelStats[levels.Length];
            for (int i = 0; i < levels.Length; i++)
                data.levels[i] = new WeaponLevelStats { damage = levels[i].damage, cooldown = levels[i].cooldown };
            return data;
        }

        [Test]
        public void BuildSteps_NoEvolution_OneStepPerLevelUp()
        {
            var weapon = Make((8f, 1.2f), (10f, 1.15f), (12f, 1.1f), (15f, 1f));

            var steps = WeaponGuideLogic.BuildSteps(weapon, null, 0, string.Empty, false);

            Assert.AreEqual(3, steps.Count);
            Assert.AreEqual(1, steps[0].FromLevel);
            Assert.AreEqual(2, steps[0].ToLevel);
            StringAssert.Contains("공격력 8 → 10", steps[0].Text);
            Assert.AreEqual(4, steps[2].ToLevel);
        }

        [Test]
        public void BuildSteps_EvolveLevel_ShowsNoteInsteadOfStats()
        {
            var weapon = Make((8f, 1.2f), (10f, 1.15f), (12f, 1.1f), (15f, 1f));

            var steps = WeaponGuideLogic.BuildSteps(weapon, null, 3, "변화!", false);

            Assert.AreEqual("변화!", steps[1].Text);
            StringAssert.Contains("공격력", steps[0].Text);
        }

        [Test]
        public void BuildSteps_AfterEvolve_UsesEvolvedWeaponStats()
        {
            var weapon = Make((8f, 1.2f), (10f, 1.15f), (12f, 1.1f), (15f, 1f));
            var evolved = Make((1f, 0.3f), (2f, 0.27f), (3f, 0.24f), (4f, 0.2f));

            var steps = WeaponGuideLogic.BuildSteps(weapon, evolved, 3, "변화!", true);

            StringAssert.Contains("우산 피해 3 → 4", steps[2].Text);
        }

        [Test]
        public void BuildSteps_NullWeapon_IsEmpty()
        {
            Assert.AreEqual(0, WeaponGuideLogic.BuildSteps(null, null, 0, string.Empty, false).Count);
        }
    }
}
