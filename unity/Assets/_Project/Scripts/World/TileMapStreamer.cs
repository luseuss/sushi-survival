using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SushiSurvival.World
{
    /// <summary>
    /// 유적 한 세트(3×3 = 9장). 깨진 유적·멀쩡한 유적처럼 종류별로 하나씩 둔다.
    /// </summary>
    [System.Serializable]
    public class RuinSet
    {
        [Tooltip("9종. 좌측 상단부터 오른쪽으로, 그다음 아래줄 순서.")]
        public Sprite[] sprites;
    }

    /// <summary>
    /// 돌길 조각 묶음. 조각마다 3×3 = 9장이고 좌측 상단부터 오른쪽으로, 그다음 아랫줄 순서다.
    /// 전부 9장이 채워져야 길을 깐다.
    /// </summary>
    [System.Serializable]
    public class RoadSprites
    {
        public Sprite[] full;
        [Tooltip("가로(좌우로 이어지는 길).")]
        public Sprite[] horizontal;
        [Tooltip("세로(위아래로 이어지는 길).")]
        public Sprite[] vertical;
        [Tooltip("길이 오른쪽으로만 이어지는 왼쪽 끝.")]
        public Sprite[] capLeft;
        [Tooltip("길이 왼쪽으로만 이어지는 오른쪽 끝.")]
        public Sprite[] capRight;
        [Tooltip("길이 아래로만 이어지는 위쪽 끝.")]
        public Sprite[] capTop;
        [Tooltip("길이 위로만 이어지는 아래쪽 끝.")]
        public Sprite[] capBottom;
        [Tooltip("사거리.")]
        public Sprite[] cross;
    }

    /// <summary>
    /// 카메라 주변으로 타일을 채우고 멀어진 영역은 비운다. 타일은 좌표 해시로
    /// 결정론적으로 고르므로, 같은 자리로 돌아오면 같은 바닥이 나온다.
    /// </summary>
    public class TileMapStreamer : MonoBehaviour
    {
        private const int RuinSetSize = 9;

        [SerializeField] private Tilemap tilemap;
        [Tooltip("따라갈 대상. 비워두면 메인 카메라를 따라간다.")]
        [SerializeField] private Transform followTarget;

        [Header("직접 칠한 패턴")]
        [Tooltip("손으로 칠한 타일맵. 지정하면 칠한 영역이 바둑판처럼 끝없이 반복된다. " +
                 "칠하지 않은 빈칸은 아래 스프라이트로 자동 채운다. 비워두면 전부 자동 생성.")]
        [SerializeField] private Tilemap paintedPattern;

        [Header("스프라이트")]
        [Tooltip("테두리 없는 잔디 16종.")]
        [SerializeField] private Sprite[] grassSprites;
        [Tooltip("꽃 등 무늬가 있는 잔디 타일. 드물게 섞인다.")]
        [SerializeField] private Sprite[] grassDetailSprites;
        [Tooltip("사막 4종.")]
        [SerializeField] private Sprite[] sandSprites;
        [Tooltip("유적 세트 목록. 세트마다 9장씩. 패치 단위로 세트를 골라 섞는다.")]
        [SerializeField] private RuinSet[] ruinSets;

        [Header("돌길")]
        [Tooltip("돌길을 따로 그릴 타일맵. 길 조각은 32px(0.32유닛)이라 바닥과 칸 크기가 다르므로, 부모 Grid의 Cell Size를 0.32로 둔 별도 Grid 아래에 만들고 Order in Layer를 바닥·장식보다 크게 둔다. Grid는 원점(0,0,0)에 둔다. 비워두면 길 없음.")]
        [SerializeField] private Tilemap roadTilemap;
        [Tooltip("돌길 조각 8종(각 9장). 하나라도 9장이 안 차면 길을 깔지 않는다.")]
        [SerializeField] private RoadSprites roadSprites;
        [Tooltip("길 배치 규칙. 길은 블록(3×3 타일) 단위의 곧은 가로·세로 줄이다.")]
        [SerializeField] private RoadConfig roadConfig = new RoadConfig
        {
            blockSize = 3,
            bandSize = 15,
            bandChance = 0.5f,
            segmentSize = 8,
            segmentChance = 0.8f
        };

        [Header("바닥 장식")]
        [Tooltip("바닥 위에 덧그릴 장식용 타일맵(꽃·사막 뼈 등). 바닥 타일맵보다 위에 그려지도록 Order in Layer를 더 크게 둔다. 비워두면 장식을 깔지 않는다.")]
        [SerializeField] private Tilemap decorTilemap;
        [Tooltip("장식 스프라이트. 확률에 걸린 바닥 칸마다 이 중 하나가 같은 확률로 뽑힌다. 바닥은 위의 잔디(기본 바닥) 스프라이트 칸에만 깐다.")]
        [SerializeField] private Sprite[] decorSprites;
        [Tooltip("기본 바닥 한 칸에 장식이 깔릴 확률. 0.06이면 열여섯 칸에 한 번쯤.")]
        [Range(0f, 1f)]
        [SerializeField] private float decorChance = 0.06f;

        [Header("생성 규칙")]
        [Tooltip("타일 한 변의 월드 크기. Grid의 Cell Size와 반드시 같아야 한다.")]
        [SerializeField] private float tileSize = 0.32f;
        [SerializeField] private int chunkSize = 16;
        [Tooltip("중심 청크로부터 이 반경만큼 유지한다.")]
        [SerializeField] private int chunkRadius = 2;
        [Tooltip("잔디 구역 한 변의 타일 수. 한 구역은 같은 타일로 채워져 색이 뭉친다.")]
        [SerializeField] private int regionSize = 4;
        [Tooltip("구역 안에서 다른 잔디 타일이 섞일 확률. 올리면 알록달록해진다.")]
        [Range(0f, 1f)]
        [SerializeField] private float grassVariantChance = 0.12f;
        [Tooltip("꽃 타일이 나올 확률.")]
        [Range(0f, 1f)]
        [SerializeField] private float grassDetailChance = 0.08f;
        [SerializeField] private float sandChance = 0.08f;
        [Tooltip("사막 덩어리 한 변의 타일 수.")]
        [SerializeField] private int sandPatchSize = 2;
        [SerializeField] private float ruinChance = 0.04f;

        [Header("시드")]
        [Tooltip("켜면 매 판 다른 맵이 나온다. 끄면 아래 시드로 고정된다.")]
        [SerializeField] private bool randomSeedEachRun = true;
        [SerializeField] private int seed = 12345;

        private readonly HashSet<Vector2Int> _loadedChunks = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> _toRemove = new List<Vector2Int>();

        private Tile[] _grassTiles;
        private Tile[] _grassDetailTiles;
        private Tile[] _sandTiles;
        private Tile[][] _ruinTileSets;
        private Tile[] _decorTiles;
        private HashSet<Sprite> _grassSpriteSet;
        private Dictionary<RoadPiece, Tile[]> _roadTiles;
        private readonly Dictionary<Vector2Int, BoundsInt> _roadBounds = new Dictionary<Vector2Int, BoundsInt>();

        private TileBase[] _patternTiles;
        private BoundsInt _patternBounds;

        private TileMixConfig _config;
        private int _activeSeed;
        private Vector2Int _lastCenterChunk;
        private bool _hasStreamedOnce;

        private void Awake()
        {
            if (tilemap == null)
            {
                Debug.LogError($"{name}: tilemap이 비어 있어 바닥을 그릴 수 없습니다.");
                enabled = false;
                return;
            }

            LoadPaintedPattern();

            if (_patternTiles == null && (grassSprites == null || grassSprites.Length == 0))
            {
                Debug.LogError($"{name}: paintedPattern과 grassSprites가 모두 비어 있어 바닥을 그릴 수 없습니다.");
                enabled = false;
                return;
            }

            if (followTarget == null && Camera.main != null)
                followTarget = Camera.main.transform;

            if (followTarget == null)
            {
                Debug.LogError($"{name}: 따라갈 대상을 찾지 못했습니다. " +
                               "followTarget을 지정하거나 카메라에 MainCamera 태그를 설정하세요.");
                enabled = false;
                return;
            }

            _activeSeed = randomSeedEachRun ? Random.Range(int.MinValue, int.MaxValue) : seed;

            _grassTiles = BuildTiles(grassSprites);
            _grassDetailTiles = BuildTiles(grassDetailSprites);
            _sandTiles = BuildTiles(sandSprites);
            _ruinTileSets = BuildRuinSets();
            _roadTiles = roadTilemap != null ? BuildRoadTiles() : null;
            _decorTiles = decorTilemap != null ? BuildTiles(decorSprites) : new Tile[0];
            _grassSpriteSet = new HashSet<Sprite>(grassSprites ?? new Sprite[0]);

            _config = new TileMixConfig
            {
                grassCount = _grassTiles.Length,
                sandCount = _sandTiles.Length,
                ruinSize = 3,
                ruinSetCount = _ruinTileSets.Length,
                sandPatchSize = sandPatchSize,
                regionSize = regionSize,
                grassVariantChance = grassVariantChance,
                grassDetailCount = _grassDetailTiles.Length,
                grassDetailChance = _grassDetailTiles.Length > 0 ? grassDetailChance : 0f,
                sandChance = _sandTiles.Length > 0 ? sandChance : 0f,
                ruinChance = _ruinTileSets.Length > 0 ? ruinChance : 0f
            };
        }

        private void LateUpdate()
        {
            if (followTarget == null) return;

            Vector2Int centerChunk = ChunkGrid.WorldToChunk(followTarget.position, chunkSize, tileSize);
            if (_hasStreamedOnce && centerChunk == _lastCenterChunk) return;

            Stream(centerChunk);

            _lastCenterChunk = centerChunk;
            _hasStreamedOnce = true;
        }

        private void Stream(Vector2Int centerChunk)
        {
            List<Vector2Int> required = ChunkGrid.GetRequiredChunks(centerChunk, chunkRadius);
            var requiredSet = new HashSet<Vector2Int>(required);

            _toRemove.Clear();
            foreach (Vector2Int loaded in _loadedChunks)
            {
                if (!requiredSet.Contains(loaded))
                    _toRemove.Add(loaded);
            }

            foreach (Vector2Int chunk in _toRemove)
            {
                ClearChunk(chunk);
                _loadedChunks.Remove(chunk);
            }

            foreach (Vector2Int chunk in required)
            {
                if (_loadedChunks.Add(chunk))
                    FillChunk(chunk);
            }
        }

        private void FillChunk(Vector2Int chunk)
        {
            int originX = chunk.x * chunkSize;
            int originY = chunk.y * chunkSize;

            var bounds = new BoundsInt(originX, originY, 0, chunkSize, chunkSize, 1);
            var tiles = new TileBase[chunkSize * chunkSize];
            TileBase[] decors = _decorTiles.Length > 0 ? new TileBase[chunkSize * chunkSize] : null;

            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    int cellX = originX + x;
                    int cellY = originY + y;

                    TileBase painted = _patternTiles != null
                        ? _patternTiles[TilePattern.IndexOf(cellX, cellY, _patternBounds)]
                        : null;

                    TileBase ground = painted != null
                        ? painted
                        : ResolveTile(TilePicker.Pick(cellX, cellY, _activeSeed, _config));

                    tiles[y * chunkSize + x] = ground;

                    if (decors != null && IsBaseGround(ground))
                    {
                        int decor = TilePicker.PickDecor(cellX, cellY, _activeSeed, _decorTiles.Length, decorChance);
                        if (decor >= 0)
                            decors[y * chunkSize + x] = _decorTiles[decor];
                    }
                }
            }

            tilemap.SetTilesBlock(bounds, tiles);

            if (decors != null)
                decorTilemap.SetTilesBlock(bounds, decors);

            FillRoadChunk(chunk);
        }

        /// <summary>
        /// 청크가 덮는 월드 영역 안의 길 칸을 roadTilemap에 깐다. 길 칸의 중심이 청크 안에 있는 칸만
        /// 맡으므로 이웃 청크와 겹치거나 비지 않는다. Grid가 원점에 있다고 가정한다.
        /// </summary>
        private void FillRoadChunk(Vector2Int chunk)
        {
            if (_roadTiles == null) return;

            float cell = roadTilemap.layoutGrid.cellSize.x;
            float worldMinX = chunk.x * chunkSize * tileSize;
            float worldMinY = chunk.y * chunkSize * tileSize;
            float worldSize = chunkSize * tileSize;

            int minX = Mathf.CeilToInt(worldMinX / cell - 0.5f);
            int minY = Mathf.CeilToInt(worldMinY / cell - 0.5f);
            int countX = Mathf.CeilToInt((worldMinX + worldSize) / cell - 0.5f) - minX;
            int countY = Mathf.CeilToInt((worldMinY + worldSize) / cell - 0.5f) - minY;

            var bounds = new BoundsInt(minX, minY, 0, countX, countY, 1);
            var tiles = new TileBase[countX * countY];
            int block = roadConfig.blockSize;

            for (int y = 0; y < countY; y++)
            {
                for (int x = 0; x < countX; x++)
                {
                    int cellX = minX + x;
                    int cellY = minY + y;

                    RoadPiece piece = RoadNetworkLogic.ChoosePiece(
                        RoadNetworkLogic.ToBlock(cellX, block), RoadNetworkLogic.ToBlock(cellY, block),
                        _activeSeed, roadConfig);

                    if (piece != RoadPiece.None)
                        tiles[y * countX + x] = _roadTiles[piece][RoadNetworkLogic.SpriteIndex(cellX, cellY, block)];
                }
            }

            roadTilemap.SetTilesBlock(bounds, tiles);
            _roadBounds[chunk] = bounds;
        }

        private void ClearChunk(Vector2Int chunk)
        {
            var bounds = new BoundsInt(chunk.x * chunkSize, chunk.y * chunkSize, 0, chunkSize, chunkSize, 1);
            tilemap.SetTilesBlock(bounds, new TileBase[chunkSize * chunkSize]);

            if (_decorTiles.Length > 0)
                decorTilemap.SetTilesBlock(bounds, new TileBase[chunkSize * chunkSize]);

            if (_roadBounds.TryGetValue(chunk, out BoundsInt roadBounds))
            {
                roadTilemap.SetTilesBlock(roadBounds, new TileBase[roadBounds.size.x * roadBounds.size.y]);
                _roadBounds.Remove(chunk);
            }
        }

        /// <summary>
        /// 장식은 기본 바닥(grassSprites) 위에만 깐다. 유적이나 이미 무늬가 그려진 손칠 타일 위에는 얹지 않는다.
        /// </summary>
        private bool IsBaseGround(TileBase tile)
            => tile is Tile t && t.sprite != null && _grassSpriteSet.Contains(t.sprite);

        private TileBase ResolveTile(TileChoice choice)
        {
            switch (choice.Kind)
            {
                case TileKind.Ruin:
                    if (_ruinTileSets.Length == 0) return Pick(_grassTiles, choice.Index);
                    Tile[] set = _ruinTileSets[Mathf.Clamp(choice.Variant, 0, _ruinTileSets.Length - 1)];
                    return Pick(set, choice.Index);

                case TileKind.GrassDetail:
                    return Pick(_grassDetailTiles, choice.Index);

                case TileKind.Sand:
                    return Pick(_sandTiles, choice.Index);

                default:
                    return Pick(_grassTiles, choice.Index);
            }
        }

        /// <summary>8종이 모두 블록 칸 수(기본 9장)만큼 채워졌을 때만 만든다. 하나라도 모자라면 길을 포기한다.</summary>
        private Dictionary<RoadPiece, Tile[]> BuildRoadTiles()
        {
            if (roadSprites == null) return null;

            int needed = Mathf.Max(1, roadConfig.blockSize) * Mathf.Max(1, roadConfig.blockSize);

            var sets = new Dictionary<RoadPiece, Sprite[]>
            {
                { RoadPiece.Full, roadSprites.full },
                { RoadPiece.Horizontal, roadSprites.horizontal },
                { RoadPiece.Vertical, roadSprites.vertical },
                { RoadPiece.CapLeft, roadSprites.capLeft },
                { RoadPiece.CapRight, roadSprites.capRight },
                { RoadPiece.CapTop, roadSprites.capTop },
                { RoadPiece.CapBottom, roadSprites.capBottom },
                { RoadPiece.Cross, roadSprites.cross }
            };

            var result = new Dictionary<RoadPiece, Tile[]>();
            foreach (KeyValuePair<RoadPiece, Sprite[]> pair in sets)
            {
                Sprite[] sprites = pair.Value;
                if (sprites == null || sprites.Length < needed)
                {
                    if (HasAnyRoadSprite())
                        Debug.LogWarning($"{name}: 돌길 {pair.Key}이 {needed}장을 채우지 못해 길을 깔지 않습니다.");
                    return null;
                }

                foreach (Sprite sprite in sprites)
                {
                    if (sprite != null) continue;
                    Debug.LogWarning($"{name}: 돌길 {pair.Key}에 빈 스프라이트 칸이 있어 길을 깔지 않습니다.");
                    return null;
                }

                result[pair.Key] = BuildTiles(sprites);
            }

            return result;
        }

        private bool HasAnyRoadSprite()
        {
            return (roadSprites.full != null && roadSprites.full.Length > 0)
                || (roadSprites.horizontal != null && roadSprites.horizontal.Length > 0)
                || (roadSprites.vertical != null && roadSprites.vertical.Length > 0)
                || (roadSprites.capLeft != null && roadSprites.capLeft.Length > 0)
                || (roadSprites.capRight != null && roadSprites.capRight.Length > 0)
                || (roadSprites.capTop != null && roadSprites.capTop.Length > 0)
                || (roadSprites.capBottom != null && roadSprites.capBottom.Length > 0)
                || (roadSprites.cross != null && roadSprites.cross.Length > 0);
        }

        private Tile Pick(Tile[] tiles, int index)
        {
            if (tiles == null || tiles.Length == 0) return null;

            return tiles[Mathf.Clamp(index, 0, tiles.Length - 1)];
        }

        /// <summary>
        /// 칠한 영역을 한 번 읽어 두고 원본 타일맵은 숨긴다. 원본을 그대로 두면
        /// 스트리밍된 바닥과 같은 자리에 두 번 그려진다.
        /// </summary>
        private void LoadPaintedPattern()
        {
            if (paintedPattern == null) return;

            paintedPattern.CompressBounds();
            BoundsInt bounds = paintedPattern.cellBounds;

            if (bounds.size.x <= 0 || bounds.size.y <= 0)
            {
                Debug.LogWarning($"{name}: paintedPattern에 칠한 타일이 없어 자동 생성으로 대신합니다.");
                return;
            }

            _patternBounds = new BoundsInt(bounds.xMin, bounds.yMin, 0, bounds.size.x, bounds.size.y, 1);
            _patternTiles = paintedPattern.GetTilesBlock(_patternBounds);

            var patternRenderer = paintedPattern.GetComponent<TilemapRenderer>();
            if (patternRenderer != null) patternRenderer.enabled = false;
        }

        /// <summary>9장이 다 채워진 세트만 쓴다. 모자란 세트는 구조물이 깨져 보인다.</summary>
        private Tile[][] BuildRuinSets()
        {
            var valid = new List<Tile[]>();

            if (ruinSets != null)
            {
                for (int i = 0; i < ruinSets.Length; i++)
                {
                    RuinSet set = ruinSets[i];
                    if (set == null || set.sprites == null || set.sprites.Length < RuinSetSize)
                    {
                        Debug.LogWarning($"{name}: 유적 세트 {i}번이 {RuinSetSize}장을 채우지 못해 건너뜁니다.");
                        continue;
                    }

                    valid.Add(BuildTiles(set.sprites));
                }
            }

            return valid.ToArray();
        }

        /// <summary>
        /// 스프라이트마다 Tile 에셋을 런타임에 만든다. 이렇게 하면 에디터용
        /// Tile Palette 패키지를 따로 설치하지 않아도 된다.
        /// </summary>
        private static Tile[] BuildTiles(Sprite[] sprites)
        {
            if (sprites == null) return new Tile[0];

            var tiles = new Tile[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprites[i];
                tiles[i] = tile;
            }

            return tiles;
        }
    }
}
