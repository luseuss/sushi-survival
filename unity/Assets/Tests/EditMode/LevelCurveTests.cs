using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class LevelCurveTests
    {
        [Test]
        public void GetRequiredXp_ReturnsBase_AtLevelOne()
        {
            Assert.That(LevelCurve.GetRequiredXp(1, 5f, 3f), Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void GetRequiredXp_GrowsByIncrementPerLevel()
        {
            Assert.That(LevelCurve.GetRequiredXp(3, 5f, 3f), Is.EqualTo(11f).Within(0.0001f));
        }

        [Test]
        public void Resolve_GainsNothing_BelowThreshold()
        {
            var result = LevelCurve.Resolve(3f, 1, 5f, 3f);

            Assert.AreEqual(0, result.LevelsGained);
            Assert.That(result.XpTowardNext, Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void Resolve_GainsOneLevel_ExactlyAtThreshold()
        {
            var result = LevelCurve.Resolve(5f, 1, 5f, 3f);

            Assert.AreEqual(1, result.LevelsGained);
            Assert.That(result.XpTowardNext, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Resolve_CarriesRemainder()
        {
            var result = LevelCurve.Resolve(7f, 1, 5f, 3f);

            Assert.AreEqual(1, result.LevelsGained);
            Assert.That(result.XpTowardNext, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void Resolve_GainsMultipleLevels_FromOneBigGem()
        {
            // Lv1 필요 5, Lv2 필요 8 → 합계 13. 15를 넣으면 2레벨 오르고 2 남는다.
            var result = LevelCurve.Resolve(15f, 1, 5f, 3f);

            Assert.AreEqual(2, result.LevelsGained);
            Assert.That(result.XpTowardNext, Is.EqualTo(2f).Within(0.0001f));
        }
    }
}
