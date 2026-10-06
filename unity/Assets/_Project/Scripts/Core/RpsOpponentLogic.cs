using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 상대 손을 승률에 맞춰 뽑는다. 비김은 1/3로 그대로 두고(재대결이라 승률에 영향 없음),
    /// 판정이 갈릴 때만 플레이어가 이길 비율을 winChance로 맞춘다.
    /// </summary>
    public static class RpsOpponentLogic
    {
        private const float DrawShare = 1f / 3f;

        /// <param name="roll">0 이상 1 미만의 난수.</param>
        public static RpsHand PickOpponentHand(RpsHand player, float winChance, float roll)
        {
            if (roll < DrawShare) return player;

            float decisive = (roll - DrawShare) / (1f - DrawShare);
            bool playerWins = decisive < Mathf.Clamp01(winChance);

            return playerWins ? HandBeatenBy(player) : HandThatBeats(player);
        }

        private static RpsHand HandBeatenBy(RpsHand hand) => hand switch
        {
            RpsHand.Rock => RpsHand.Scissors,
            RpsHand.Paper => RpsHand.Rock,
            _ => RpsHand.Paper
        };

        private static RpsHand HandThatBeats(RpsHand hand) => hand switch
        {
            RpsHand.Rock => RpsHand.Paper,
            RpsHand.Paper => RpsHand.Scissors,
            _ => RpsHand.Rock
        };
    }
}
