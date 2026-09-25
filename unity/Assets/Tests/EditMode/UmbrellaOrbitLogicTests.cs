using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class UmbrellaOrbitLogicTests
    {
        [Test]
        public void AngleStepDegrees_DividesFullCircleEvenly()
        {
            Assert.AreEqual(90f, UmbrellaOrbitLogic.AngleStepDegrees(4), 0.001f);
            Assert.AreEqual(72f, UmbrellaOrbitLogic.AngleStepDegrees(5), 0.001f);
        }

        [Test]
        public void AngleStepDegrees_ZeroCount_ReturnsZero()
        {
            Assert.AreEqual(0f, UmbrellaOrbitLogic.AngleStepDegrees(0), 0.001f);
        }

        [Test]
        public void AngleForIndex_SpreadsEvenlyFromBaseAngle()
        {
            Assert.AreEqual(0f, UmbrellaOrbitLogic.AngleForIndex(0f, 0, 4), 0.001f);
            Assert.AreEqual(90f, UmbrellaOrbitLogic.AngleForIndex(0f, 1, 4), 0.001f);
            Assert.AreEqual(270f, UmbrellaOrbitLogic.AngleForIndex(90f, 2, 4), 0.001f);
        }

        [Test]
        public void PositionForAngle_ZeroDegrees_IsAlongPositiveX()
        {
            Vector2 pos = UmbrellaOrbitLogic.PositionForAngle(0f, 2f);
            Assert.AreEqual(2f, pos.x, 0.001f);
            Assert.AreEqual(0f, pos.y, 0.001f);
        }

        [Test]
        public void PositionForAngle_NinetyDegrees_IsAlongPositiveY()
        {
            Vector2 pos = UmbrellaOrbitLogic.PositionForAngle(90f, 2f);
            Assert.AreEqual(0f, pos.x, 0.001f);
            Assert.AreEqual(2f, pos.y, 0.001f);
        }
    }
}
