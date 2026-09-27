using NUnit.Framework;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class PlayerTraitStateTests
    {
        // 정적 상태라 테스트끼리 서로 영향을 주지 않도록 매번 비운다.
        [SetUp]
        public void SetUp() => PlayerTraitState.Reset();

        [TearDown]
        public void TearDown() => PlayerTraitState.Reset();

        [Test]
        public void Record_AddsPicksInOrder()
        {
            PlayerTraitState.Record(PlayerTrait.Bold);
            PlayerTraitState.Record(PlayerTrait.Kind);

            Assert.AreEqual(2, PlayerTraitState.Picks.Count);
            Assert.AreEqual(PlayerTrait.Bold, PlayerTraitState.Picks[0]);
            Assert.AreEqual(PlayerTrait.Kind, PlayerTraitState.Picks[1]);
        }

        [Test]
        public void Record_IgnoresNone()
        {
            PlayerTraitState.Record(PlayerTrait.None);

            Assert.AreEqual(0, PlayerTraitState.Picks.Count);
        }

        [Test]
        public void Dominant_UsesTheLogic()
        {
            PlayerTraitState.Record(PlayerTrait.Bold);
            PlayerTraitState.Record(PlayerTrait.Careful);

            // 동수라 가장 나중에 고른 성향.
            Assert.AreEqual(PlayerTrait.Careful, PlayerTraitState.Dominant);
        }

        [Test]
        public void Reset_ClearsPicks()
        {
            PlayerTraitState.Record(PlayerTrait.Bold);
            PlayerTraitState.Reset();

            Assert.AreEqual(0, PlayerTraitState.Picks.Count);
            Assert.AreEqual(PlayerTrait.None, PlayerTraitState.Dominant);
        }
    }
}
