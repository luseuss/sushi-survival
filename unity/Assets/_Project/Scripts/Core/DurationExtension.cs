using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>여러 이벤트가 겹칠 때 정지·흔들림이 쌓여 버벅이지 않도록,
    /// 남은 시간을 새 요청과 합치지 않고 더 긴 쪽으로 늘린다.</summary>
    public static class DurationExtension
    {
        public static float Extend(float remaining, float requested)
            => Mathf.Max(remaining, requested);
    }
}
