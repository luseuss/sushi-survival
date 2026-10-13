using NUnit.Framework;
using SushiSurvival.Companions;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class FairyDescriptionLogicTests
    {
        private static WeaponLevelStats Stats(float damage, float cooldown, float range)
            => new WeaponLevelStats { damage = damage, cooldown = cooldown, range = range };

        [Test]
        public void IncludesRoleThenStats()
        {
            string text = FairyDescriptionLogic.DescribeSummon("고화력 느림", Stats(10f, 1.8f, 6f));

            Assert.AreEqual("고화력 느림\n피해 10 · 쿨타임 1.8초 · 사거리 6", text);
        }

        [Test]
        public void EmptyRole_ShowsOnlyStatsLine()
        {
            string text = FairyDescriptionLogic.DescribeSummon("", Stats(5f, 1f, 6f));

            Assert.AreEqual("피해 5 · 쿨타임 1초 · 사거리 6", text);
        }

        [Test]
        public void NullRole_IsTreatedAsEmpty()
        {
            string text = FairyDescriptionLogic.DescribeSummon(null, Stats(5f, 1f, 6f));

            Assert.AreEqual("피해 5 · 쿨타임 1초 · 사거리 6", text);
        }

        [Test]
        public void DescribeStats_IsStatsLineOnly()
        {
            Assert.AreEqual("피해 6 · 쿨타임 1.3초 · 사거리 7", FairyDescriptionLogic.DescribeStats(Stats(6f, 1.3f, 7f)));
        }

        [Test]
        public void DescribeSummon_EndsWithTheStatsLine()
        {
            WeaponLevelStats level = Stats(4f, 0.8f, 6f);

            StringAssert.EndsWith(FairyDescriptionLogic.DescribeStats(level),
                                  FairyDescriptionLogic.DescribeSummon("중간 연사", level));
        }

        [Test]
        public void RoundsToTwoDecimals()
        {
            string text = FairyDescriptionLogic.DescribeSummon("x", Stats(4.126f, 0.6f, 5f));

            StringAssert.Contains("피해 4.13", text);
            StringAssert.Contains("쿨타임 0.6초", text);
        }
    }
}
