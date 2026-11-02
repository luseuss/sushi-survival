using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class VolumeDisplayLogicTests
    {
        [Test]
        public void PercentText_RoundsToWholePercent()
        {
            Assert.AreEqual("0%", VolumeDisplayLogic.PercentText(0f));
            Assert.AreEqual("70%", VolumeDisplayLogic.PercentText(0.7f));
            Assert.AreEqual("100%", VolumeDisplayLogic.PercentText(1f));
            Assert.AreEqual("33%", VolumeDisplayLogic.PercentText(0.3333f));
        }

        [Test]
        public void PercentText_ClampsOutOfRange()
        {
            Assert.AreEqual("0%", VolumeDisplayLogic.PercentText(-0.5f));
            Assert.AreEqual("100%", VolumeDisplayLogic.PercentText(2f));
        }
    }
}
