using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class AffinityBuffLogicTests
    {
        [Test]
        public void GetBuffAmount_ReturnsFractionOfCap()
        {
            Assert.AreEqual(0.25f, AffinityBuffLogic.GetBuffAmount(2f, 0.125f), 0.0001f);
        }

        [Test]
        public void GetBuffAmount_ZeroCap_ReturnsZero()
        {
            Assert.AreEqual(0f, AffinityBuffLogic.GetBuffAmount(0f, 0.125f), 0.0001f);
        }

        [Test]
        public void GetBuffAmount_NegativeCap_ClampsToZero()
        {
            // 데이터 입력 실수로 음수 maxCap이 들어와도 음수 버프를 주면 안 된다.
            Assert.AreEqual(0f, AffinityBuffLogic.GetBuffAmount(-5f, 0.125f), 0.0001f);
        }

        [Test]
        public void GetBuffAmount_RatioAboveOne_ClampsToOne()
        {
            Assert.AreEqual(2f, AffinityBuffLogic.GetBuffAmount(2f, 1.5f), 0.0001f);
        }

        [Test]
        public void GetBuffAmount_NegativeRatio_ClampsToZero()
        {
            Assert.AreEqual(0f, AffinityBuffLogic.GetBuffAmount(2f, -0.5f), 0.0001f);
        }

        [Test]
        public void GetBuffAmount_ZeroRatio_ReturnsZero()
        {
            Assert.AreEqual(0f, AffinityBuffLogic.GetBuffAmount(2f, 0f), 0.0001f);
        }
    }
}
