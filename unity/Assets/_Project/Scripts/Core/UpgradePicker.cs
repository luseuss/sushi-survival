using System.Collections.Generic;

namespace SushiSurvival.Core
{
    public static class UpgradePicker
    {
        /// <summary>
        /// 후보에서 중복 없이 count개를 무작위로 뽑는다. 후보가 모자라면
        /// 있는 만큼만 돌려준다. 원본 리스트는 건드리지 않는다.
        /// </summary>
        public static List<T> PickDistinct<T>(IReadOnlyList<T> candidates, int count, System.Random random)
        {
            var pool = new List<T>(candidates);
            var picked = new List<T>();

            int take = count < pool.Count ? count : pool.Count;
            for (int i = 0; i < take; i++)
            {
                int index = random.Next(pool.Count);
                picked.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return picked;
        }
    }
}
