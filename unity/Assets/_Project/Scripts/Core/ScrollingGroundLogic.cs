namespace SushiSurvival.Core
{
    /// <summary>
    /// 끝없이 이어 붙인 땅 그림의 이동 계산. 한 장 폭만큼 밀리면 처음으로 돌아가므로 이음새가 같은 자리에 머문다.
    /// </summary>
    public static class ScrollingGroundLogic
    {
        /// <summary>지금 위치에서 delta만큼 더 밀린 위치. 0 이상 tileWidth 미만으로 유지한다.</summary>
        public static float Advance(float offset, float delta, float tileWidth)
        {
            if (tileWidth <= 0f) return 0f;

            float next = (offset + delta) % tileWidth;
            return next < 0f ? next + tileWidth : next;
        }
    }
}
