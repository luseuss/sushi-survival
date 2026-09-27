using NUnit.Framework;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class StoryChoiceLogicTests
    {
        private static StoryChoicePoint Point(int beforeLineIndex)
            => new StoryChoicePoint { beforeLineIndex = beforeLineIndex };

        [Test]
        public void FindPointIndex_ReturnsThePointBeforeThatLine()
        {
            var points = new[] { Point(4), Point(8) };

            Assert.AreEqual(0, StoryChoiceLogic.FindPointIndex(points, 4));
            Assert.AreEqual(1, StoryChoiceLogic.FindPointIndex(points, 8));
        }

        [Test]
        public void FindPointIndex_NoPointForThatLine_ReturnsMinusOne()
        {
            var points = new[] { Point(4), Point(8) };

            Assert.AreEqual(-1, StoryChoiceLogic.FindPointIndex(points, 5));
        }

        [Test]
        public void FindPointIndex_SeveralPointsOnTheSameLine_ReturnsTheFirst()
        {
            var points = new[] { Point(3), Point(4), Point(4) };

            Assert.AreEqual(1, StoryChoiceLogic.FindPointIndex(points, 4));
        }

        [Test]
        public void FindPointIndex_EmptyList_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, StoryChoiceLogic.FindPointIndex(new StoryChoicePoint[0], 0));
        }

        [Test]
        public void FindPointIndex_NullList_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, StoryChoiceLogic.FindPointIndex(null, 0));
        }

        [Test]
        public void FindPointIndex_SkipsNullEntries()
        {
            var points = new[] { null, Point(4) };

            Assert.AreEqual(1, StoryChoiceLogic.FindPointIndex(points, 4));
        }
    }
}
