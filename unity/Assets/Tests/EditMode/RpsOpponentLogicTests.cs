using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class RpsOpponentLogicTests
    {
        private static readonly RpsHand[] AllHands = { RpsHand.Rock, RpsHand.Paper, RpsHand.Scissors };

        [Test]
        public void RollInFirstThird_IsAlwaysADraw()
        {
            foreach (RpsHand player in AllHands)
            {
                Assert.AreEqual(RpsOutcome.Draw,
                    RpsRules.Resolve(player, RpsOpponentLogic.PickOpponentHand(player, 0.4f, 0f)));
                Assert.AreEqual(RpsOutcome.Draw,
                    RpsRules.Resolve(player, RpsOpponentLogic.PickOpponentHand(player, 0.4f, 0.33f)));
            }
        }

        [Test]
        public void WinChanceOne_DecisiveRollsAlwaysWin()
        {
            foreach (RpsHand player in AllHands)
            {
                foreach (float roll in new[] { 0.34f, 0.6f, 0.99f })
                {
                    Assert.AreEqual(RpsOutcome.Win,
                        RpsRules.Resolve(player, RpsOpponentLogic.PickOpponentHand(player, 1f, roll)));
                }
            }
        }

        [Test]
        public void WinChanceZero_DecisiveRollsAlwaysLose()
        {
            foreach (RpsHand player in AllHands)
            {
                foreach (float roll in new[] { 0.34f, 0.6f, 0.99f })
                {
                    Assert.AreEqual(RpsOutcome.Lose,
                        RpsRules.Resolve(player, RpsOpponentLogic.PickOpponentHand(player, 0f, roll)));
                }
            }
        }

        [Test]
        public void EvenlySpacedRolls_MatchExpectedRatios()
        {
            const int n = 9000;
            const float winChance = 0.4f;
            int draws = 0, wins = 0, losses = 0;

            for (int i = 0; i < n; i++)
            {
                float roll = (i + 0.5f) / n;
                switch (RpsRules.Resolve(RpsHand.Rock, RpsOpponentLogic.PickOpponentHand(RpsHand.Rock, winChance, roll)))
                {
                    case RpsOutcome.Draw: draws++; break;
                    case RpsOutcome.Win: wins++; break;
                    default: losses++; break;
                }
            }

            Assert.AreEqual(1f / 3f, draws / (float)n, 0.01f);
            // 비기면 다시 하므로 최종 승률 = 판정이 갈린 것 중 이긴 비율.
            Assert.AreEqual(winChance, wins / (float)(wins + losses), 0.01f);
        }

        [Test]
        public void WinChance_OutOfRange_IsClamped()
        {
            Assert.AreEqual(RpsOutcome.Win,
                RpsRules.Resolve(RpsHand.Paper, RpsOpponentLogic.PickOpponentHand(RpsHand.Paper, 5f, 0.9f)));
            Assert.AreEqual(RpsOutcome.Lose,
                RpsRules.Resolve(RpsHand.Paper, RpsOpponentLogic.PickOpponentHand(RpsHand.Paper, -2f, 0.9f)));
        }
    }
}
