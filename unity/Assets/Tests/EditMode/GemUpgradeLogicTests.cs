using NUnit.Framework;
using SushiSurvival.Data;
using SushiSurvival.Enemies;

namespace SushiSurvival.EditModeTests
{
    public class GemUpgradeLogicTests
    {
        private const float UpgradeAt = 180f;

        [Test]
        public void Resolve_ReturnsBase_BeforeUpgradeTime()
        {
            Assert.AreEqual(XPGemType.Basic,
                GemUpgradeLogic.Resolve(XPGemType.Basic, XPGemType.Five, UpgradeAt, 179f));
        }

        [Test]
        public void Resolve_ReturnsUpgraded_AtUpgradeTime()
        {
            Assert.AreEqual(XPGemType.Five,
                GemUpgradeLogic.Resolve(XPGemType.Basic, XPGemType.Five, UpgradeAt, 180f));
        }

        [Test]
        public void Resolve_ReturnsUpgraded_AfterUpgradeTime()
        {
            Assert.AreEqual(XPGemType.Five,
                GemUpgradeLogic.Resolve(XPGemType.Basic, XPGemType.Five, UpgradeAt, 300f));
        }

        [Test]
        public void Resolve_NeverUpgrades_WhenTimeIsZero()
        {
            // 0은 "승급 없음"이다. 이게 대부분의 몹의 기본 상태다.
            Assert.AreEqual(XPGemType.Basic,
                GemUpgradeLogic.Resolve(XPGemType.Basic, XPGemType.Ten, 0f, 999f));
        }

        [Test]
        public void Resolve_NeverUpgrades_WhenTimeIsNegative()
        {
            Assert.AreEqual(XPGemType.Basic,
                GemUpgradeLogic.Resolve(XPGemType.Basic, XPGemType.Ten, -5f, 999f));
        }

        [Test]
        public void Resolve_WorksForAnyGemPair()
        {
            Assert.AreEqual(XPGemType.Ten,
                GemUpgradeLogic.Resolve(XPGemType.Five, XPGemType.Ten, 240f, 240f));
        }
    }
}
