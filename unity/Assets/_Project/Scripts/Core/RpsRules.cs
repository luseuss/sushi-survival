namespace SushiSurvival.Core
{
    public enum RpsHand
    {
        Rock,
        Paper,
        Scissors
    }

    public enum RpsOutcome
    {
        Win,
        Lose,
        Draw
    }

    /// <summary>
    /// 가위바위보 승패 판정. 비기면 Draw — 호출 쪽에서 재대결로 처리한다.
    /// </summary>
    public static class RpsRules
    {
        public static RpsOutcome Resolve(RpsHand player, RpsHand opponent)
        {
            if (player == opponent) return RpsOutcome.Draw;

            bool win = (player == RpsHand.Rock && opponent == RpsHand.Scissors)
                       || (player == RpsHand.Paper && opponent == RpsHand.Rock)
                       || (player == RpsHand.Scissors && opponent == RpsHand.Paper);

            return win ? RpsOutcome.Win : RpsOutcome.Lose;
        }
    }
}
