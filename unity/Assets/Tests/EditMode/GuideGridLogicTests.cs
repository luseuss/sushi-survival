using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class GuideGridLogicTests
    {
        private static readonly Vector2 First = new Vector2(-604f, 222f);
        private static readonly Vector2 Pitch = new Vector2(223f, 182.5f);

        [Test]
        public void SlotCenter_FillsRowsLeftToRight_ThenDownwards()
        {
            Assert.AreEqual(First, GuideGridLogic.SlotCenter(0, First, Pitch, 4));
            Assert.AreEqual(new Vector2(-604f + 223f, 222f), GuideGridLogic.SlotCenter(1, First, Pitch, 4));
            Assert.AreEqual(new Vector2(-604f, 222f - 182.5f), GuideGridLogic.SlotCenter(4, First, Pitch, 4));
            Assert.AreEqual(new Vector2(-604f + 3 * 223f, 222f - 3 * 182.5f), GuideGridLogic.SlotCenter(15, First, Pitch, 4));
        }

        [Test]
        public void Capacity_IsSixteen()
        {
            Assert.AreEqual(16, GuideGridLogic.Capacity);
        }

        [Test]
        public void Move_StepsToNeighbours()
        {
            Assert.AreEqual(1, GuideGridLogic.Move(0, 1, 0, 4, 4, 10));
            Assert.AreEqual(4, GuideGridLogic.Move(0, 0, 1, 4, 4, 10));
            Assert.AreEqual(5, GuideGridLogic.Move(4, 1, 0, 4, 4, 10));
        }

        [Test]
        public void Move_StaysAtEdges()
        {
            Assert.AreEqual(0, GuideGridLogic.Move(0, -1, 0, 4, 4, 10));
            Assert.AreEqual(0, GuideGridLogic.Move(0, 0, -1, 4, 4, 10));
            Assert.AreEqual(3, GuideGridLogic.Move(3, 1, 0, 4, 4, 10));
        }

        [Test]
        public void Move_DoesNotLandOnEmptySlots()
        {
            // 10칸이면 9번이 마지막 — 그 오른쪽(10번)이나 그 아래(13번)는 비어 있다.
            Assert.AreEqual(9, GuideGridLogic.Move(9, 1, 0, 4, 4, 10));
            Assert.AreEqual(9, GuideGridLogic.Move(9, 0, 1, 4, 4, 10));
            Assert.AreEqual(9, GuideGridLogic.Move(5, 0, 1, 4, 4, 10));
        }

        [Test]
        public void Move_WithNoSelection_GoesToFirst_AndNoItemsGivesMinusOne()
        {
            Assert.AreEqual(0, GuideGridLogic.Move(-1, 1, 0, 4, 4, 10));
            Assert.AreEqual(-1, GuideGridLogic.Move(0, 1, 0, 4, 4, 0));
        }
    }
}
