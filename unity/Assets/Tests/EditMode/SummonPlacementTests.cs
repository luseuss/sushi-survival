using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Enemies.Boss;

namespace SushiSurvival.EditModeTests
{
    public class SummonPlacementTests
    {
        private static readonly Vector2 Center = new Vector2(3f, -7f);
        private const float Radius = 4f;

        [Test]
        public void GetPositions_ReturnsRequestedCount()
        {
            Assert.AreEqual(4, SummonPlacement.GetPositions(Center, 4, Radius, 0f).Count);
            Assert.AreEqual(6, SummonPlacement.GetPositions(Center, 6, Radius, 0f).Count);
        }

        [Test]
        public void GetPositions_AllSitOnTheRing()
        {
            foreach (Vector2 position in SummonPlacement.GetPositions(Center, 6, Radius, 0.4f))
                Assert.AreEqual(Radius, Vector2.Distance(Center, position), 0.001f);
        }

        [Test]
        public void GetPositions_AreEvenlySpaced()
        {
            // 뭉쳐서 나오면 한쪽만 막으면 되는 패턴이 되어 위협이 사라진다.
            List<Vector2> positions = SummonPlacement.GetPositions(Center, 4, Radius, 0f);

            float expectedGap = Vector2.Distance(positions[0], positions[1]);
            for (int i = 0; i < positions.Count; i++)
            {
                Vector2 next = positions[(i + 1) % positions.Count];
                Assert.AreEqual(expectedGap, Vector2.Distance(positions[i], next), 0.001f);
            }
        }

        [Test]
        public void GetPositions_NeverOverlapEachOther()
        {
            List<Vector2> positions = SummonPlacement.GetPositions(Center, 6, Radius, 0f);

            for (int i = 0; i < positions.Count; i++)
                for (int j = i + 1; j < positions.Count; j++)
                    Assert.Greater(Vector2.Distance(positions[i], positions[j]), 0.5f);
        }

        [Test]
        public void GetPositions_StartAngleRotatesTheWholeRing()
        {
            // 매번 같은 자리에 솟아나면 외워져서 긴장이 사라진다.
            List<Vector2> a = SummonPlacement.GetPositions(Center, 4, Radius, 0f);
            List<Vector2> b = SummonPlacement.GetPositions(Center, 4, Radius, 0.5f);

            for (int i = 0; i < a.Count; i++)
                Assert.Greater(Vector2.Distance(a[i], b[i]), 0.01f);
        }

        [Test]
        public void GetPositions_ReturnsEmpty_WhenCountIsZeroOrNegative()
        {
            Assert.AreEqual(0, SummonPlacement.GetPositions(Center, 0, Radius, 0f).Count);
            Assert.AreEqual(0, SummonPlacement.GetPositions(Center, -3, Radius, 0f).Count);
        }
    }
}
