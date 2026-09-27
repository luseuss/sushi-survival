using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class PlayerTraitLogicTests
    {
        [Test]
        public void Dominant_EmptyList_IsNone()
        {
            Assert.AreEqual(PlayerTrait.None, PlayerTraitLogic.Dominant(new List<PlayerTrait>()));
        }

        [Test]
        public void Dominant_NullList_IsNone()
        {
            Assert.AreEqual(PlayerTrait.None, PlayerTraitLogic.Dominant(null));
        }

        [Test]
        public void Dominant_MostChosenWins()
        {
            var picks = new List<PlayerTrait> { PlayerTrait.Kind, PlayerTrait.Bold, PlayerTrait.Bold };
            Assert.AreEqual(PlayerTrait.Bold, PlayerTraitLogic.Dominant(picks));
        }

        [Test]
        public void Dominant_TwoWayTie_LatestPickWins()
        {
            var picks = new List<PlayerTrait> { PlayerTrait.Bold, PlayerTrait.Careful };
            Assert.AreEqual(PlayerTrait.Careful, PlayerTraitLogic.Dominant(picks));
        }

        [Test]
        public void Dominant_ThreeWayTie_LatestPickWins()
        {
            var picks = new List<PlayerTrait> { PlayerTrait.Bold, PlayerTrait.Careful, PlayerTrait.Kind };
            Assert.AreEqual(PlayerTrait.Kind, PlayerTraitLogic.Dominant(picks));
        }

        [Test]
        public void Dominant_IgnoresNone()
        {
            var picks = new List<PlayerTrait> { PlayerTrait.None, PlayerTrait.Bold, PlayerTrait.None };
            Assert.AreEqual(PlayerTrait.Bold, PlayerTraitLogic.Dominant(picks));

            var onlyNone = new List<PlayerTrait> { PlayerTrait.None, PlayerTrait.None };
            Assert.AreEqual(PlayerTrait.None, PlayerTraitLogic.Dominant(onlyNone));
        }
    }
}
