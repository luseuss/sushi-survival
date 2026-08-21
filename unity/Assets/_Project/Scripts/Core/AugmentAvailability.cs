namespace SushiSurvival.Core
{
    public static class AugmentAvailability
    {
        /// <summary>
        /// 누적값이 상한에 닿으면 후보에서 뺀다. 마지막 한 번이 상한을 살짝
        /// 넘기는 것은 허용한다 — StatSystem이 어차피 클램프한다.
        /// </summary>
        public static bool IsAvailable(float accumulated, float maxCap)
            => accumulated < maxCap;
    }
}
