using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class CircleTextureFactoryTests
    {
        [Test]
        public void IsInsideBand_Disc_IncludesCenter()
        {
            // 원판(innerRatio 0)은 중심부터 가장자리까지 전부 채운다.
            Assert.IsTrue(CircleTextureFactory.IsInsideBand(0f, 0f));
            Assert.IsTrue(CircleTextureFactory.IsInsideBand(0.5f, 0f));
            Assert.IsTrue(CircleTextureFactory.IsInsideBand(1f, 0f));
        }

        [Test]
        public void IsInsideBand_Ring_ExcludesCenter()
        {
            float inner = CircleTextureFactory.RingInnerRatio;

            Assert.IsFalse(CircleTextureFactory.IsInsideBand(0f, inner));
            Assert.IsFalse(CircleTextureFactory.IsInsideBand(0.5f, inner));
            Assert.IsTrue(CircleTextureFactory.IsInsideBand(0.9f, inner));
            Assert.IsTrue(CircleTextureFactory.IsInsideBand(1f, inner));
        }

        [Test]
        public void IsInsideBand_ExcludesOutsideTheCircle()
        {
            // 정사각 텍스처의 모서리는 반경 밖이라 반드시 투명해야 한다.
            Assert.IsFalse(CircleTextureFactory.IsInsideBand(1.01f, 0f));
            Assert.IsFalse(CircleTextureFactory.IsInsideBand(1.41f, 0f));
        }

        [Test]
        public void IsInsideBand_IncludesTheInnerEdgeItself()
        {
            Assert.IsTrue(CircleTextureFactory.IsInsideBand(0.85f, 0.85f));
        }

        [Test]
        public void GetNormalizedDistance_IsZeroAtCenter()
        {
            Assert.AreEqual(0f, CircleTextureFactory.GetNormalizedDistance(32, 32, 64), 0.03f);
        }

        [Test]
        public void GetNormalizedDistance_IsOneAtEdgeMidpoint()
        {
            Assert.AreEqual(1f, CircleTextureFactory.GetNormalizedDistance(0, 32, 64), 0.03f);
        }

        [Test]
        public void GetNormalizedDistance_ExceedsOneAtCorners()
        {
            Assert.Greater(CircleTextureFactory.GetNormalizedDistance(0, 0, 64), 1.3f);
        }
    }
}
