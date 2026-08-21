namespace SushiSurvival.World
{
    /// <summary>
    /// 좌표를 결정론적으로 해시한다. 같은 좌표는 언제나 같은 값을 내놓아야
    /// 청크를 버렸다 다시 만들어도 바닥이 그대로 유지된다.
    /// </summary>
    public static class TileHash
    {
        public static uint Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)seed;
                h ^= (uint)x * 2654435761u;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 2246822519u;
                h = (h << 17) | (h >> 15);
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return h;
            }
        }

        /// <summary>0~1 범위로 정규화한 값.</summary>
        public static float Normalized(int x, int y, int seed)
            => Hash(x, y, seed) / (float)uint.MaxValue;

        /// <summary>0~count-1 범위의 인덱스. count가 0 이하면 0을 돌려준다.</summary>
        public static int Index(int x, int y, int seed, int count)
            => count <= 0 ? 0 : (int)(Hash(x, y, seed) % (uint)count);
    }
}
