using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class StoryDialogueLogicTests
    {
        // ---- NextIndex ----

        [Test]
        public void NextIndex_MovesToNextLine()
        {
            Assert.AreEqual(1, StoryDialogueLogic.NextIndex(0, 3));
        }

        [Test]
        public void NextIndex_ReturnsCount_WhenAdvancingPastLastLine()
        {
            Assert.AreEqual(3, StoryDialogueLogic.NextIndex(2, 3));
        }

        [Test]
        public void NextIndex_ClampsToCount_WhenAlreadyPastEnd()
        {
            Assert.AreEqual(3, StoryDialogueLogic.NextIndex(3, 3));
            Assert.AreEqual(3, StoryDialogueLogic.NextIndex(5, 3));
        }

        [Test]
        public void NextIndex_ReturnsZero_ForEmptyData()
        {
            Assert.AreEqual(0, StoryDialogueLogic.NextIndex(0, 0));
            Assert.AreEqual(0, StoryDialogueLogic.NextIndex(-1, 0));
        }

        [Test]
        public void NextIndex_TreatsNegativeCurrentAsBeforeFirst()
        {
            Assert.AreEqual(0, StoryDialogueLogic.NextIndex(-1, 3));
            Assert.AreEqual(0, StoryDialogueLogic.NextIndex(-5, 3));
        }

        // ---- IsFinished ----

        [Test]
        public void IsFinished_False_WhileLinesRemain()
        {
            Assert.IsFalse(StoryDialogueLogic.IsFinished(0, 3));
            Assert.IsFalse(StoryDialogueLogic.IsFinished(2, 3));
        }

        [Test]
        public void IsFinished_True_AtOrPastEnd()
        {
            Assert.IsTrue(StoryDialogueLogic.IsFinished(3, 3));
            Assert.IsTrue(StoryDialogueLogic.IsFinished(4, 3));
        }

        [Test]
        public void IsFinished_True_ForEmptyData()
        {
            Assert.IsTrue(StoryDialogueLogic.IsFinished(0, 0));
            Assert.IsTrue(StoryDialogueLogic.IsFinished(-1, 0));
        }

        // ---- ResolveBackgroundIndex ----

        [Test]
        public void ResolveBackgroundIndex_ReturnsMinusOne_WhenNoBackgroundDefined()
        {
            Assert.AreEqual(-1, StoryDialogueLogic.ResolveBackgroundIndex(new[] { false, false }, 1));
        }

        [Test]
        public void ResolveBackgroundIndex_ReturnsSameIndex_WhenThatLineDefinesOne()
        {
            Assert.AreEqual(0, StoryDialogueLogic.ResolveBackgroundIndex(new[] { true, false, false }, 0));
        }

        [Test]
        public void ResolveBackgroundIndex_ReturnsNearestPrecedingDefinition()
        {
            var has = new[] { true, false, true, false };

            Assert.AreEqual(0, StoryDialogueLogic.ResolveBackgroundIndex(has, 1));
            Assert.AreEqual(2, StoryDialogueLogic.ResolveBackgroundIndex(has, 3));
        }

        [Test]
        public void ResolveBackgroundIndex_IgnoresLaterDefinitions()
        {
            Assert.AreEqual(-1, StoryDialogueLogic.ResolveBackgroundIndex(new[] { false, false, true }, 1));
        }

        [Test]
        public void ResolveBackgroundIndex_ClampsIndexBeyondEnd()
        {
            Assert.AreEqual(0, StoryDialogueLogic.ResolveBackgroundIndex(new[] { true, false }, 9));
        }

        [Test]
        public void ResolveBackgroundIndex_ReturnsMinusOne_ForInvalidInput()
        {
            Assert.AreEqual(-1, StoryDialogueLogic.ResolveBackgroundIndex(null, 0));
            Assert.AreEqual(-1, StoryDialogueLogic.ResolveBackgroundIndex(new bool[0], 0));
            Assert.AreEqual(-1, StoryDialogueLogic.ResolveBackgroundIndex(new[] { true }, -1));
        }

        // ---- IsBackgroundChange ----

        [Test]
        public void IsBackgroundChange_True_OnlyOnLinesThatDefineOne()
        {
            var has = new[] { true, false, true };

            Assert.IsTrue(StoryDialogueLogic.IsBackgroundChange(has, 0));
            Assert.IsFalse(StoryDialogueLogic.IsBackgroundChange(has, 1));
            Assert.IsTrue(StoryDialogueLogic.IsBackgroundChange(has, 2));
        }

        [Test]
        public void IsBackgroundChange_False_ForOutOfRangeOrNull()
        {
            var has = new[] { true, true, true };

            Assert.IsFalse(StoryDialogueLogic.IsBackgroundChange(has, -1));
            Assert.IsFalse(StoryDialogueLogic.IsBackgroundChange(has, 3));
            Assert.IsFalse(StoryDialogueLogic.IsBackgroundChange(null, 0));
        }
    }
}
