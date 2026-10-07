using NUnit.Framework;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class ShotgunProgressionLogicTests
    {
        [TestCase(1, 3, false)]
        [TestCase(2, 3, false)]
        [TestCase(3, 3, true)]
        [TestCase(4, 3, true)]
        public void ShouldEvolve_TrueOnceLevelReachesThreshold(int level, int evolveLevel, bool expected)
        {
            Assert.AreEqual(expected, ShotgunProgressionLogic.ShouldEvolve(level, evolveLevel));
        }

        [Test]
        public void DescribeRifleUpgrade_KeepsStatText_WhenNotEvolving()
        {
            Assert.AreEqual("공격력 12 → 14",
                ShotgunProgressionLogic.DescribeRifleUpgrade("공격력 12 → 14", false, 5));
        }

        [Test]
        public void DescribeRifleUpgrade_ReplacesStatText_WithTransformNotice_WhenEvolving()
        {
            string text = ShotgunProgressionLogic.DescribeRifleUpgrade("공격력 14 → 17", true, 5);

            StringAssert.Contains("샷건으로 변화", text);
            StringAssert.Contains("산탄 5발", text);
            StringAssert.DoesNotContain("공격력", text);
        }
    }
}
