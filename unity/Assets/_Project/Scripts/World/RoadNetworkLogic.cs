namespace SushiSurvival.World
{
    public enum RoadPiece
    {
        None,
        /// <summary>돌이 꽉 찬 조각. 코너·T자처럼 맞는 그림이 없는 칸의 대체.</summary>
        Full,
        /// <summary>좌우로 이어지는 가로 길.</summary>
        Horizontal,
        /// <summary>위아래로 이어지는 세로 길.</summary>
        Vertical,
        /// <summary>길이 오른쪽으로만 이어지는 왼쪽 끝.</summary>
        CapLeft,
        /// <summary>길이 왼쪽으로만 이어지는 오른쪽 끝.</summary>
        CapRight,
        /// <summary>길이 아래로만 이어지는 위쪽 끝.</summary>
        CapTop,
        /// <summary>길이 위로만 이어지는 아래쪽 끝.</summary>
        CapBottom,
        /// <summary>사방으로 이어지는 사거리.</summary>
        Cross
    }

    [System.Serializable]
    public struct RoadConfig
    {
        [UnityEngine.Tooltip("길 한 칸(블록)의 한 변 타일 수. 아트가 3×3이므로 3.")]
        public int blockSize;
        [UnityEngine.Tooltip("띠 한 장의 폭(블록 수). 띠마다 가로 길·세로 길이 최대 하나씩 생긴다.")]
        public int bandSize;
        [UnityEngine.Tooltip("띠에 길이 생길 확률.")]
        public float bandChance;
        [UnityEngine.Tooltip("길을 토막 내는 구간 길이(블록 수). 구간이 꺼지면 끊기고 끝 조각이 붙는다.")]
        public int segmentSize;
        [UnityEngine.Tooltip("구간이 이어질 확률.")]
        public float segmentChance;
    }

    /// <summary>
    /// 월드 전체에 곧은 가로·세로 길을 시드 해시로 정한다. 길은 블록(3×3 타일) 단위이고,
    /// 블록의 이웃 연결에 따라 그릴 조각을 고른다. 좌표만으로 결정되므로 청크를 버려도 같다.
    /// </summary>
    public static class RoadNetworkLogic
    {
        private const int HorizontalBandSeedOffset = 179426549;
        private const int HorizontalRowSeedOffset = 198491317;
        private const int HorizontalSegmentSeedOffset = 217645177;
        private const int VerticalBandSeedOffset = 236887691;
        private const int VerticalColumnSeedOffset = 256203161;
        private const int VerticalSegmentSeedOffset = 275604541;

        public static RoadConfig DefaultConfig => new RoadConfig
        {
            blockSize = 3,
            bandSize = 15,
            bandChance = 0.5f,
            segmentSize = 8,
            segmentChance = 0.8f
        };

        public static int ToBlock(int cell, int blockSize) => FloorDiv(cell, AtLeastOne(blockSize));

        /// <summary>블록 좌표가 길인지.</summary>
        public static bool IsRoadBlock(int bx, int by, int seed, RoadConfig config)
        {
            if (config.bandChance <= 0f) return false;

            return IsLine(by, bx, seed + HorizontalBandSeedOffset, seed + HorizontalRowSeedOffset,
                          seed + HorizontalSegmentSeedOffset, config)
                || IsLine(bx, by, seed + VerticalBandSeedOffset, seed + VerticalColumnSeedOffset,
                          seed + VerticalSegmentSeedOffset, config);
        }

        /// <summary>
        /// across: 길과 수직인 좌표(가로 길이면 y), along: 길을 따라가는 좌표(가로 길이면 x).
        /// 띠 안에서 길의 위치는 양 끝 한 칸을 비운 자리라서, 이웃 띠의 길과 최소 3블록 떨어진다.
        /// </summary>
        private static bool IsLine(int across, int along, int bandSeed, int rowSeed, int segmentSeed, RoadConfig config)
        {
            int band = AtLeastOne(config.bandSize);
            int bandIndex = FloorDiv(across, band);

            if (TileHash.Normalized(bandIndex, 0, bandSeed) >= config.bandChance) return false;

            int row = band >= 3 ? 1 + TileHash.Index(bandIndex, 0, rowSeed, band - 2) : 0;
            if (Mod(across, band) != row) return false;

            int segment = FloorDiv(along, AtLeastOne(config.segmentSize));
            return TileHash.Normalized(bandIndex, segment, segmentSeed) < config.segmentChance;
        }

        /// <summary>블록에 그릴 조각. 길이 아니면 None.</summary>
        public static RoadPiece ChoosePiece(int bx, int by, int seed, RoadConfig config)
        {
            if (!IsRoadBlock(bx, by, seed, config)) return RoadPiece.None;

            bool left = IsRoadBlock(bx - 1, by, seed, config);
            bool right = IsRoadBlock(bx + 1, by, seed, config);
            bool down = IsRoadBlock(bx, by - 1, seed, config);
            bool up = IsRoadBlock(bx, by + 1, seed, config);

            int count = (left ? 1 : 0) + (right ? 1 : 0) + (down ? 1 : 0) + (up ? 1 : 0);

            if (count == 4) return RoadPiece.Cross;
            if (count == 2 && left && right) return RoadPiece.Horizontal;
            if (count == 2 && up && down) return RoadPiece.Vertical;

            if (count == 1)
            {
                if (right) return RoadPiece.CapLeft;
                if (left) return RoadPiece.CapRight;
                if (down) return RoadPiece.CapTop;
                return RoadPiece.CapBottom;
            }

            return RoadPiece.Full;
        }

        /// <summary>
        /// 블록 안에서 몇 번째 스프라이트인지(0~blockSize²-1). 스프라이트는 좌측 상단부터
        /// 오른쪽으로, 그다음 아랫줄 순이라 월드 y(위가 +)를 뒤집어야 한다.
        /// </summary>
        public static int SpriteIndex(int x, int y, int blockSize)
        {
            int size = AtLeastOne(blockSize);
            return (size - 1 - Mod(y, size)) * size + Mod(x, size);
        }

        private static int AtLeastOne(int value) => value > 0 ? value : 1;

        private static int FloorDiv(int a, int b)
        {
            int q = a / b;
            return (a % b != 0 && (a < 0) != (b < 0)) ? q - 1 : q;
        }

        private static int Mod(int a, int b)
        {
            int r = a % b;
            return r < 0 ? r + b : r;
        }
    }
}
