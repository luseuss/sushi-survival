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
    /// 카메라 주변으로 타일을 채우고 멀어진 영역은 비운다. 타일은 좌표 해시로
    /// 결정론적으로 고르므로, 같은 자리로 돌아오면 같은 바닥이 나온다.
    /// </summary>
    public class TileMapStreamer : MonoBehaviour
    {
        private const int RuinSetSize = 9;

        [SerializeField] private Tilemap tilemap;
        [Tooltip("따라갈 대상. 비워두면 메인 카메라를 따라간다.")]
        [SerializeField] private Transform followTarget;

        [Header("스프라이트")]
        [Tooltip("테두리 없는 잔디 16종.")]
        [SerializeField] private Sprite[] grassSprites;
        [Tooltip("꽃 등 무늬가 있는 잔디 타일. 드물게 섞인다.")]
        [SerializeField] private Sprite[] grassDetailSprites;
        [Tooltip("사막 4종.")]
        [SerializeField] private Sprite[] sandSprites;
        [Tooltip("유적 세트 목록. 세트마다 9장씩. 패치 단위로 세트를 골라 섞는다.")]
        [SerializeField] private RuinSet[] ruinSets;

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

            if (grassSprites == null || grassSprites.Length == 0)
            {
                Debug.LogError($"{name}: grassSprites가 비어 있어 바닥을 그릴 수 없습니다.");
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

            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    TileChoice choice = TilePicker.Pick(originX + x, originY + y, _activeSeed, _config);
                    tiles[y * chunkSize + x] = ResolveTile(choice);
                }
            }

            tilemap.SetTilesBlock(bounds, tiles);
        }

        private void ClearChunk(Vector2Int chunk)
        {
            var bounds = new BoundsInt(chunk.x * chunkSize, chunk.y * chunkSize, 0, chunkSize, chunkSize, 1);
            tilemap.SetTilesBlock(bounds, new TileBase[chunkSize * chunkSize]);
        }

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

        private Tile Pick(Tile[] tiles, int index)
        {
            if (tiles == null || tiles.Length == 0) return null;

            return tiles[Mathf.Clamp(index, 0, tiles.Length - 1)];
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
