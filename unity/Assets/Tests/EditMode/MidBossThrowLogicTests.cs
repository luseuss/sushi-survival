using NUnit.Framework;
using SushiSurvival.Enemies;

namespace SushiSurvival.EditModeTests
{
    public class MidBossThrowLogicTests
    {
        [TestCase(2.9f, false)]
        [TestCase(3f, true)]
        [TestCase(6f, true)]
        [TestCase(9f, true)]
        [TestCase(9.1f, false)]
        public void InThrowRange_IncludesBothBounds(float distance, bool expected)
        {
            Assert.AreEqual(expected, MidBossThrowLogic.InThrowRange(distance, 3f, 9f));
        }
    }
}
