namespace SushiSurvival.Core
{
    /// <summary>
    /// 흔들림 지속시간은 여러 히트가 겹치며 도중에 늘어날 수 있어(DurationExtension),
    /// "경과시간 대비 진폭"으로 감쇠 곡선을 그리면 기준점이 계속 바뀐다. 대신
    /// 남은 시간이 짧은 구간에서만 0으로 선형 감쇠한다.
    /// </summary>
    public static class CameraShakeLogic
    {
        /// <summary>남은 시간이 이 값 아래로 내려가면 그 구간에서 0으로 선형 감쇠한다.</summary>
        public const float FalloffTail = 0.05f;

        public static float GetMagnitude(float remaining, float peakMagnitude)
        {
            if (remaining <= 0f) return 0f;
            if (remaining >= FalloffTail) return peakMagnitude;

            return peakMagnitude * (remaining / FalloffTail);
        }
    }
}
