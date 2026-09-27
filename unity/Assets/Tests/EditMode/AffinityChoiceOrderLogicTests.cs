using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class AffinityChoiceOrderLogicTests
    {
        private static readonly PlayerTrait[] Traits = { PlayerTrait.Bold, PlayerTrait.Careful, PlayerTrait.Kind };

        [Test]
        public void RecommendedIndex_ReturnsTheMatchingIndex()
        {
            Assert.AreEqual(2, AffinityChoiceOrderLogic.RecommendedIndex(Traits, PlayerTrait.Kind));
        }

        [Test]
        public void RecommendedIndex_NoMatch_ReturnsMinusOne()
        {
            var traits = new List<PlayerTrait> { PlayerTrait.Bold, PlayerTrait.Careful };
            Assert.AreEqual(-1, AffinityChoiceOrderLogic.RecommendedIndex(traits, PlayerTrait.Kind));
        }

        [Test]
        public void RecommendedIndex_DominantNone_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, AffinityChoiceOrderLogic.RecommendedIndex(Traits, PlayerTrait.None));
        }

        [Test]
        public void RecommendedIndex_DuplicateTraits_ReturnsTheFirst()
        {
            var traits = new List<PlayerTrait> { PlayerTrait.Kind, PlayerTrait.Bold, PlayerTrait.Bold };
            Assert.AreEqual(1, AffinityChoiceOrderLogic.RecommendedIndex(traits, PlayerTrait.Bold));
        }

        [Test]
        public void RecommendedIndex_NullTraits_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, AffinityChoiceOrderLogic.RecommendedIndex(null, PlayerTrait.Bold));
        }

        [Test]
        public void DisplayOrder_MovesRecommendedToFront_KeepingOthersInOrder()
        {
            Assert.AreEqual(new[] { 2, 0, 1 }, AffinityChoiceOrderLogic.DisplayOrder(3, 2));
            Assert.AreEqual(new[] { 1, 0, 2 }, AffinityChoiceOrderLogic.DisplayOrder(3, 1));
        }

        [Test]
        public void DisplayOrder_NoRecommendation_IsIdentity()
        {
            Assert.AreEqual(new[] { 0, 1, 2 }, AffinityChoiceOrderLogic.DisplayOrder(3, -1));
            Assert.AreEqual(new[] { 0, 1, 2 }, AffinityChoiceOrderLogic.DisplayOrder(3, 0));
        }

        [Test]
        public void DisplayOrder_IndexOutOfRange_IsIdentity()
        {
            Assert.AreEqual(new[] { 0, 1, 2 }, AffinityChoiceOrderLogic.DisplayOrder(3, 5));
        }

        [Test]
        public void DisplayOrder_ZeroCount_IsEmpty()
        {
            Assert.AreEqual(0, AffinityChoiceOrderLogic.DisplayOrder(0, 0).Length);
        }
    }
}
