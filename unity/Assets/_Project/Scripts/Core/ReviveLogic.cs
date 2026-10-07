namespace SushiSurvival.Core
{
    public static class ReviveLogic
    {
        /// <summary>이미 쓴 부활 횟수가 허용 횟수(부활 증강 스탯)보다 적을 때만 한 번 더 부활할 수 있다.</summary>
        public static bool CanRevive(int used, int allowed) => used < allowed;
    }
}
