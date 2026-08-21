using UnityEngine;

namespace SushiSurvival.World
{
    public enum TileKind
    {
        Grass,
        /// <summary>꽃 등 눈에 띄는 무늬 타일. 기본 잔디보다 드물게 나온다.</summary>
        GrassDetail,
        Sand,
        Ruin
    }

    public struct TileChoice
    {
        public TileKind Kind;

        /// <summary>해당 종류의 스프라이트 배열 안에서의 인덱스.</summary>
        public int Index;

        /// <summary>유적 전용 — 몇 번째 유적 세트(깨짐/멀쩡)를 쓸지.</summary>
        public int Variant;
    }

    [System.Serializable]
    public struct TileMixConfig
    {
        public int grassCount;
        public int sandCount;

        [Tooltip("유적 패치 한 변의 타일 수. 아트가 3×3이므로 3.")]
        public int ruinSize;
        [Tooltip("유적 세트 수(깨짐·멀쩡 등).")]
        public int ruinSetCount;

        [Tooltip("사막 덩어리 한 변의 타일 수. 1이면 낱개로 흩뿌려져 노이즈처럼 보인다.")]
        public int sandPatchSize;

        [Tooltip("잔디 구역 한 변의 타일 수. 한 구역은 같은 타일로 채워져 색이 뭉친다.")]
        public int regionSize;
        [Tooltip("구역 안에서 다른 잔디 타일이 섞일 확률. 1이면 칸마다 완전 무작위가 된다.")]
        public float grassVariantChance;

        [Tooltip("꽃 등 무늬 타일의 개수.")]
        public int grassDetailCount;
        [Tooltip("무늬 타일이 나올 확률. 0.08이면 열두 칸에 한 번쯤 꽃이 보인다.")]
        public float grassDetailChance;

        public float sandChance;
        public float ruinChance;
    }

    public static class TilePicker
    {
        // 종류마다 다른 해시 계열을 쓰기 위한 오프셋. 같은 시드로 여러 판정을
        // 할 때 값이 상관관계를 갖지 않도록 서로 다른 소수를 더한다.
        private const int RuinSeedOffset = 7919;
        private const int RuinVariantSeedOffset = 1299709;
        private const int SandSeedOffset = 104729;
        private const int SandIndexSeedOffset = 15485863;
        private const int RegionSeedOffset = 32452843;
        private const int GrassVariantSeedOffset = 49979687;
        private const int GrassSpeckleSeedOffset = 67867967;
        private const int GrassDetailSeedOffset = 86028121;
        private const int GrassDetailIndexSeedOffset = 104395301;

        public static TileChoice Pick(int x, int y, int seed, TileMixConfig config)
        {
            int ruinSize = AtLeastOne(config.ruinSize);

            // 유적은 3×3이 이어진 하나의 구조물이므로 패치 격자 단위로 판정한다.
            int ruinPatchX = FloorDiv(x, ruinSize);
            int ruinPatchY = FloorDiv(y, ruinSize);

            if (TileHash.Normalized(ruinPatchX, ruinPatchY, seed + RuinSeedOffset) < config.ruinChance)
            {
                return new TileChoice
                {
                    Kind = TileKind.Ruin,
                    Index = Mod(y, ruinSize) * ruinSize + Mod(x, ruinSize),
                    // 세트도 패치 단위로 고른다. 한 패치 안에서 세트가 섞이면
                    // 깨진 유적과 멀쩡한 유적이 반씩 붙어 구조물이 갈려 보인다.
                    Variant = TileHash.Index(ruinPatchX, ruinPatchY, seed + RuinVariantSeedOffset, config.ruinSetCount)
                };
            }

            // 사막도 덩어리로 묶는다. 낱개로 흩뿌리면 노이즈처럼 보인다.
            int sandPatchSize = AtLeastOne(config.sandPatchSize);
            int sandPatchX = FloorDiv(x, sandPatchSize);
            int sandPatchY = FloorDiv(y, sandPatchSize);

            if (TileHash.Normalized(sandPatchX, sandPatchY, seed + SandSeedOffset) < config.sandChance)
            {
                return new TileChoice
                {
                    Kind = TileKind.Sand,
                    Index = TileHash.Index(x, y, seed + SandIndexSeedOffset, config.sandCount)
                };
            }

            // 대부분은 무늬 없는 기본 잔디. 꽃은 드물게 섞여야 시선을 뺏지 않는다.
            if (config.grassDetailCount > 0 &&
                TileHash.Normalized(x, y, seed + GrassDetailSeedOffset) < config.grassDetailChance)
            {
                return new TileChoice
                {
                    Kind = TileKind.GrassDetail,
                    Index = TileHash.Index(x, y, seed + GrassDetailIndexSeedOffset, config.grassDetailCount)
                };
            }

            return new TileChoice
            {
                Kind = TileKind.Grass,
                Index = PickGrassIndex(x, y, seed, config)
            };
        }

        /// <summary>
        /// 구역마다 주 타일을 정하고 그 타일로 채운다. 아주 가끔만 다른 타일을
        /// 섞어 단조로움을 없앤다. 칸마다 무작위로 뽑으면 서로 다른 색이
        /// 체커보드처럼 튀어서 눈이 아프다.
        /// </summary>
        private static int PickGrassIndex(int x, int y, int seed, TileMixConfig config)
        {
            if (TileHash.Normalized(x, y, seed + GrassVariantSeedOffset) < config.grassVariantChance)
                return TileHash.Index(x, y, seed + GrassSpeckleSeedOffset, config.grassCount);

            int regionSize = AtLeastOne(config.regionSize);
            int regionX = FloorDiv(x, regionSize);
            int regionY = FloorDiv(y, regionSize);

            return TileHash.Index(regionX, regionY, seed + RegionSeedOffset, config.grassCount);
        }

        private static int AtLeastOne(int value) => value > 0 ? value : 1;

        /// <summary>음수에서도 아래로 내림하는 나눗셈. C#의 / 는 0쪽으로 자른다.</summary>
        private static int FloorDiv(int a, int b) => Mathf.FloorToInt(a / (float)b);

        /// <summary>항상 0 이상을 돌려주는 나머지.</summary>
        private static int Mod(int a, int b)
        {
            int r = a % b;
            return r < 0 ? r + b : r;
        }
    }
}
