using System.Collections.Generic;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 잡몹 지속 스폰에서 지금 뽑을 수 있는 풀을 가린다. 풀마다 "이 시각부터 나온다"는 값을 두어
    /// 초반엔 기본 잡몹만, 시간이 지나면 다른 잡몹이 섞이게 한다.
    /// </summary>
    public static class SpawnPoolLogic
    {
        /// <summary>
        /// elapsedSeconds 시점에 나올 수 있는 풀의 인덱스. unlockSeconds가 비었거나 풀보다 짧으면
        /// 값이 없는 풀은 처음부터 나오는 것으로 본다. 어느 풀도 열리지 않았으면 0번을 돌려주어 스폰이 끊기지 않게 한다.
        /// </summary>
        public static List<int> AvailableIndices(float elapsedSeconds, IReadOnlyList<float> unlockSeconds, int poolCount)
        {
            var available = new List<int>();

            for (int i = 0; i < poolCount; i++)
            {
                float unlock = unlockSeconds != null && i < unlockSeconds.Count ? unlockSeconds[i] : 0f;
                if (elapsedSeconds >= unlock) available.Add(i);
            }

            if (available.Count == 0 && poolCount > 0) available.Add(0);
            return available;
        }
    }
}
