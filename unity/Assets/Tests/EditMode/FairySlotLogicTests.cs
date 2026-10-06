using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Companions;

namespace SushiSurvival.EditModeTests
{
    public class FairySlotLogicTests
    {
        private const float Radius = 1.2f;
        private const float Arc = 60f;

        [Test]
        public void SingleFairy_SitsDirectlyAbovePlayer()
        {
            Vector2 offset = FairySlotLogic.SlotOffset(0, 1, Radius, Arc, 0f, 0f, 1f);

            Assert.AreEqual(0f, offset.x, 0.001f);
            Assert.AreEqual(Radius, offset.y, 0.001f);
        }

        [Test]
        public void ThreeFairies_FanOutSymmetricallyAroundTop()
        {
            Vector2 left = FairySlotLogic.SlotOffset(0, 3, Radius, Arc, 0f, 0f, 1f);
            Vector2 middle = FairySlotLogic.SlotOffset(1, 3, Radius, Arc, 0f, 0f, 1f);
            Vector2 right = FairySlotLogic.SlotOffset(2, 3, Radius, Arc, 0f, 0f, 1f);

            Assert.AreEqual(0f, middle.x, 0.001f);
            Assert.AreEqual(-left.x, right.x, 0.001f);
            Assert.AreEqual(left.y, right.y, 0.001f);
            Assert.Greater(right.x, 0f);
        }

        [Test]
        public void SlotsAreDistinct()
        {
            for (int count = 2; count <= 3; count++)
            {
                for (int i = 0; i < count; i++)
                {
                    for (int j = i + 1; j < count; j++)
                    {
                        Vector2 a = FairySlotLogic.SlotOffset(i, count, Radius, Arc, 0f, 0f, 1f);
                        Vector2 b = FairySlotLogic.SlotOffset(j, count, Radius, Arc, 0f, 0f, 1f);
                        Assert.Greater((a - b).magnitude, 0.3f, $"count={count}, {i} vs {j}");
                    }
                }
            }
        }

        [Test]
        public void Bob_StaysWithinAmplitude()
        {
            Vector2 rest = FairySlotLogic.SlotOffset(0, 2, Radius, Arc, 0f, 0f, 2f);

            for (float t = 0f; t < 10f; t += 0.1f)
            {
                Vector2 bobbing = FairySlotLogic.SlotOffset(0, 2, Radius, Arc, t, 0.15f, 2f);
                Assert.LessOrEqual(Mathf.Abs(bobbing.y - rest.y), 0.15f + 0.0001f);
                Assert.AreEqual(rest.x, bobbing.x, 0.0001f);
            }
        }

        [Test]
        public void NoFairies_IsZero()
        {
            Assert.AreEqual(Vector2.zero, FairySlotLogic.SlotOffset(0, 0, Radius, Arc, 0f, 0f, 1f));
        }

        [Test]
        public void OutOfRangeIndex_IsClamped()
        {
            Vector2 last = FairySlotLogic.SlotOffset(2, 3, Radius, Arc, 0f, 0f, 1f);
            Assert.AreEqual(last, FairySlotLogic.SlotOffset(9, 3, Radius, Arc, 0f, 0f, 1f));
            Assert.AreEqual(FairySlotLogic.SlotOffset(0, 3, Radius, Arc, 0f, 0f, 1f),
                            FairySlotLogic.SlotOffset(-4, 3, Radius, Arc, 0f, 0f, 1f));
        }

        [Test]
        public void Follow_ZeroDelta_StaysPut()
        {
            Assert.AreEqual(new Vector2(1f, 2f),
                FairySlotLogic.Follow(new Vector2(1f, 2f), new Vector2(9f, 9f), 8f, 0f));
        }

        [Test]
        public void Follow_HugeSharpness_ReachesTarget()
        {
            Vector2 result = FairySlotLogic.Follow(Vector2.zero, new Vector2(3f, -2f), 1000f, 1f);
            Assert.AreEqual(3f, result.x, 0.001f);
            Assert.AreEqual(-2f, result.y, 0.001f);
        }

        [Test]
        public void Follow_IsFrameRateIndependent()
        {
            Vector2 target = new Vector2(4f, 0f);

            Vector2 oneStep = FairySlotLogic.Follow(Vector2.zero, target, 6f, 0.2f);
            Vector2 twoSteps = FairySlotLogic.Follow(
                FairySlotLogic.Follow(Vector2.zero, target, 6f, 0.1f), target, 6f, 0.1f);

            Assert.AreEqual(oneStep.x, twoSteps.x, 0.0001f);
        }
    }
}
