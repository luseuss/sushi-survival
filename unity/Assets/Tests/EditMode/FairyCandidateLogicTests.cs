using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Companions;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class FairyCandidateLogicTests
    {
        private static readonly int[] Bold = { 8, 3, 4 };
        private static readonly int[] Careful = { 0, 2, 1 };
        private static readonly int[] Kind = { 5, 6, 7 };
        private static readonly int[] Neutral = { 1, 5, 0 };

        private static List<int> For(PlayerTrait trait, int catalog = 9)
            => FairyCandidateLogic.ForTrait(trait, Bold, Careful, Kind, Neutral, catalog);

        [Test]
        public void EachTrait_GetsItsOwnThreeCandidates()
        {
            CollectionAssert.AreEqual(Bold, For(PlayerTrait.Bold));
            CollectionAssert.AreEqual(Careful, For(PlayerTrait.Careful));
            CollectionAssert.AreEqual(Kind, For(PlayerTrait.Kind));
        }

        [Test]
        public void NoTrait_UsesNeutralCandidates()
        {
            CollectionAssert.AreEqual(Neutral, For(PlayerTrait.None));
        }

        [Test]
        public void OutOfRangeAndDuplicateIndices_AreDropped()
        {
            var result = FairyCandidateLogic.ForTrait(PlayerTrait.Bold, new[] { 8, 8, 12, -1, 3 }, null, null, null, 9);
            CollectionAssert.AreEqual(new[] { 8, 3 }, result);
        }

        [Test]
        public void MoreThanThree_AreTrimmedToThree()
        {
            var result = FairyCandidateLogic.ForTrait(PlayerTrait.Kind, null, null, new[] { 0, 1, 2, 3, 4 }, null, 9);
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void MissingArray_GivesEmptyList()
        {
            Assert.IsEmpty(FairyCandidateLogic.ForTrait(PlayerTrait.Bold, null, null, null, null, 9));
        }

        [Test]
        public void PromptText_ShowsProgress()
        {
            StringAssert.Contains("(1/2)", FairyCandidateLogic.PromptText(PlayerTrait.Bold, 1, 2));
            StringAssert.Contains("(2/2)", FairyCandidateLogic.PromptText(PlayerTrait.None, 2, 2));
        }

        [Test]
        public void BuildUpgrades_ListsOnlyFairiesBelowMaxLevel()
        {
            var result = FairyChoiceLogic.BuildUpgrades(new[] { 4, 2 }, 4);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(FairyChoiceKind.Upgrade, result[0].Kind);
            Assert.AreEqual(1, result[0].Index);
        }

        [Test]
        public void BuildUpgrades_NoFairies_ReturnsEmpty()
        {
            Assert.IsEmpty(FairyChoiceLogic.BuildUpgrades(null, 4));
            Assert.IsEmpty(FairyChoiceLogic.BuildUpgrades(new int[0], 4));
        }

        [Test]
        public void FixedCount_IsTwo()
        {
            Assert.AreEqual(2, FairyChoiceLogic.FixedCount);
        }
    }
}
