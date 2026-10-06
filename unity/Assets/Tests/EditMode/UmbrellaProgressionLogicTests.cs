using NUnit.Framework;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class UmbrellaProgressionLogicTests
    {
        private static readonly int[] Hits = { 1, 2, 2, 2 };

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(4, 2)]
        [TestCase(9, 2)]
        [TestCase(0, 1)]
        public void HitCount_FollowsTableAndClamps(int level, int expected)
        {
            Assert.AreEqual(expected, UmbrellaProgressionLogic.HitCount(level, Hits));
        }

        [Test]
        public void HitCount_EmptyTable_IsOne()
        {
            Assert.AreEqual(1, UmbrellaProgressionLogic.HitCount(3, null));
            Assert.AreEqual(1, UmbrellaProgressionLogic.HitCount(3, new int[0]));
        }

        [Test]
        public void HitCount_NeverBelowOne()
        {
            Assert.AreEqual(1, UmbrellaProgressionLogic.HitCount(1, new[] { 0 }));
        }

        [TestCase(1, 3, false)]
        [TestCase(2, 3, false)]
        [TestCase(3, 3, true)]
        [TestCase(4, 3, true)]
        public void ShouldEvolve_AtOrAboveEvolveLevel(int level, int evolveLevel, bool expected)
        {
            Assert.AreEqual(expected, UmbrellaProgressionLogic.ShouldEvolve(level, evolveLevel));
        }

        [Test]
        public void DescribeEggUpgrade_WhenEvolving_ShowsTransformNoticeOnly()
        {
            string text = UmbrellaProgressionLogic.DescribeEggUpgrade("공격력 8 → 12", 2, 2, true);

            StringAssert.Contains("회전 우산", text);
            StringAssert.DoesNotContain("공격력", text);
        }

        [Test]
        public void DescribeEggUpgrade_MoreHits_AppendsHitLine()
        {
            Assert.AreEqual("공격력 8 → 10\n2연타", UmbrellaProgressionLogic.DescribeEggUpgrade("공격력 8 → 10", 1, 2, false));
        }

        [Test]
        public void DescribeEggUpgrade_MoreHitsWithoutStats_ShowsHitLineOnly()
        {
            Assert.AreEqual("2연타", UmbrellaProgressionLogic.DescribeEggUpgrade("", 1, 2, false));
        }

        [Test]
        public void DescribeEggUpgrade_SameHits_KeepsStatText()
        {
            Assert.AreEqual("공격력 8 → 10", UmbrellaProgressionLogic.DescribeEggUpgrade("공격력 8 → 10", 2, 2, false));
        }

        [Test]
        public void DescribeUmbrellaCount_Changed_AppendsLine()
        {
            Assert.AreEqual("우산 피해 5 → 8\n우산 3개 → 5개", UmbrellaProgressionLogic.DescribeUmbrellaCount("우산 피해 5 → 8", 3, 5));
        }

        [Test]
        public void DescribeUmbrellaCount_Unchanged_KeepsStatText()
        {
            Assert.AreEqual("x", UmbrellaProgressionLogic.DescribeUmbrellaCount("x", 3, 3));
        }
    }
}
