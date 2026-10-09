using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Player;

namespace SushiSurvival.Companions
{
    /// <summary>
    /// 펫(요정) 시스템 총괄 — 씬 오브젝트(캐릭터 프리팹에 붙이지 않는다). 카탈로그(펫 종류 목록)를 들고
    /// 요정마다 (종류 번호, 레벨)을 관리하며, 소환·강화·보스 씬 복원·카드 문구를 제공한다.
    /// </summary>
    public class FairyController : MonoBehaviour
    {
        [Tooltip("펫 종류 목록. 소환 카드는 이 중 아직 안 가진 종류에서 무작위로 뽑는다. 비어 있으면 요정 대신 스탯 버프 보상이 나온다.")]
        [SerializeField] private FairyData[] catalog;
        [Tooltip("요정 투사체 풀. 풀 하나당 GameObject 하나(같은 오브젝트에 풀을 둘 붙이지 말 것).")]
        [SerializeField] private GameObjectPool projectilePool;
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("요정 프리팹(SpriteRenderer + Fairy). 비워두면 SpriteRenderer를 런타임에 만든다.")]
        [SerializeField] private Fairy fairyPrefab;
        [SerializeField] private int maxFairies = 3;
        [SerializeField] private FairyMotion motion = FairyMotion.Default;

        private readonly List<Fairy> _fairies = new List<Fairy>();
        private readonly List<int> _kinds = new List<int>();
        private readonly List<int> _levelsCache = new List<int>();
        private readonly System.Random _random = new System.Random();
        private Transform _player;
        private PlayerStats _stats;

        public int Count => _fairies.Count;
        public int MaxCount => maxFairies;

        public int MaxLevel
        {
            get
            {
                if (catalog == null || catalog.Length == 0) return 1;

                int min = int.MaxValue;
                foreach (FairyData data in catalog)
                {
                    if (data == null || data.levels == null) continue;
                    min = Mathf.Min(min, data.levels.Length);
                }

                return min == int.MaxValue ? 1 : min;
            }
        }

        /// <summary>요정마다의 현재 레벨(소환 순서). 호출마다 내부 리스트를 다시 채워 돌려주므로 보관하려면 복사한다.</summary>
        public IReadOnlyList<int> Levels
        {
            get
            {
                _levelsCache.Clear();
                foreach (Fairy fairy in _fairies)
                    _levelsCache.Add(fairy.Level);
                return _levelsCache;
            }
        }

        /// <summary>요정마다의 카탈로그 번호(소환 순서).</summary>
        public IReadOnlyList<int> Kinds => _kinds;

        public void SetPlayer(Transform player, PlayerStats stats)
        {
            _player = player;
            _stats = stats;

            foreach (Fairy fairy in _fairies)
                fairy.SetPlayer(player, stats);
        }

        /// <summary>kindIndex번 펫을 Lv1로 소환한다. 가득 찼거나 이미 가졌거나 잘못된 번호면 false.</summary>
        public bool Summon(int kindIndex)
        {
            if (_fairies.Count >= maxFairies) return false;
            if (_kinds.Contains(kindIndex)) return false;

            FairyData data = DataOf(kindIndex);
            if (data == null)
            {
                Debug.LogError($"{name}: 카탈로그 {kindIndex}번이 비어 있어 펫을 소환할 수 없습니다.");
                return false;
            }

            Fairy fairy = CreateFairy(data);
            _fairies.Add(fairy);
            _kinds.Add(kindIndex);
            fairy.Initialize(_player, _stats, data, projectilePool, enemyLayer, motion,
                             1, _fairies.Count - 1, _fairies.Count);

            RefreshSlots();
            return true;
        }

        /// <summary>index번 요정을 한 단계 강화한다. 최대 레벨이거나 없는 번호면 false.</summary>
        public bool Upgrade(int index)
        {
            if (index < 0 || index >= _fairies.Count) return false;

            Fairy fairy = _fairies[index];
            if (fairy.Level >= MaxLevel) return false;

            fairy.SetLevel(fairy.Level + 1);
            return true;
        }

        /// <summary>
        /// 기존 요정을 비우고 (종류, 레벨) 배열대로 다시 만든다(보스 씬 복원용). 둘 중 하나가 null이거나
        /// 길이가 다르면 아무것도 하지 않는다. SetPlayer가 먼저 불려 있어야 요정이 플레이어 곁에서 시작한다.
        /// </summary>
        public void Restore(IReadOnlyList<int> kinds, IReadOnlyList<int> levels)
        {
            if (kinds == null || levels == null || kinds.Count != levels.Count) return;

            foreach (Fairy fairy in _fairies)
            {
                if (fairy != null) Destroy(fairy.gameObject);
            }
            _fairies.Clear();
            _kinds.Clear();

            for (int i = 0; i < kinds.Count; i++)
            {
                if (!Summon(kinds[i])) continue;

                _fairies[_fairies.Count - 1].SetLevel(levels[i]);
            }
        }

        public List<FairyChoice> BuildChoices()
            => FairyChoiceLogic.Build(_kinds, Levels, catalog != null ? catalog.Length : 0,
                                      maxFairies, MaxLevel, _random);

        public string NameOfKind(int kindIndex)
        {
            FairyData data = DataOf(kindIndex);
            return data != null ? data.displayName : string.Empty;
        }

        /// <summary>index번 요정의 펫 이름.</summary>
        public string NameOfFairy(int index)
            => index >= 0 && index < _kinds.Count ? NameOfKind(_kinds[index]) : string.Empty;

        /// <summary>카드 아이콘으로 쓸 펫의 첫 프레임. 프레임이 없으면 null.</summary>
        public Sprite IconOfKind(int kindIndex)
        {
            FairyData data = DataOf(kindIndex);
            return data != null && data.frames != null && data.frames.Length > 0 ? data.frames[0] : null;
        }

        public string DescribeSummon(int kindIndex)
        {
            FairyData data = DataOf(kindIndex);
            if (data == null || data.levels == null || data.levels.Length == 0) return string.Empty;

            return FairyDescriptionLogic.DescribeSummon(data.roleLine, data.levels[0]);
        }

        /// <summary>다음 레벨과의 수치 비교 문구. 최대 레벨이거나 없는 번호면 빈 문자열.</summary>
        public string DescribeUpgrade(int index)
        {
            if (index < 0 || index >= _fairies.Count) return string.Empty;

            FairyData data = DataOf(_kinds[index]);
            int level = _fairies[index].Level;
            if (data == null || data.levels == null || level >= data.levels.Length) return string.Empty;

            return UpgradeDescriptionLogic.DescribeWeaponUpgrade(data.levels[level - 1], data.levels[level]);
        }

        private FairyData DataOf(int kindIndex)
            => catalog != null && kindIndex >= 0 && kindIndex < catalog.Length ? catalog[kindIndex] : null;

        private void RefreshSlots()
        {
            for (int i = 0; i < _fairies.Count; i++)
                _fairies[i].SetSlot(i, _fairies.Count);
        }

        private Fairy CreateFairy(FairyData data)
        {
            Fairy fairy;

            if (fairyPrefab != null)
            {
                fairy = Instantiate(fairyPrefab, transform);
            }
            else
            {
                var go = new GameObject($"Fairy ({data.displayName})");
                go.transform.SetParent(transform, false);
                go.AddComponent<SpriteRenderer>();
                fairy = go.AddComponent<Fairy>();
            }

            var spriteRenderer = fairy.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = fairy.gameObject.AddComponent<SpriteRenderer>();

            // 프레임이 있으면 첫 프레임, 아직 없으면 노란 원으로 대신해 아트 연결 전에도 보이게 한다.
            spriteRenderer.sprite = data.frames != null && data.frames.Length > 0
                ? data.frames[0]
                : CircleTextureFactory.CreateSprite(24, 0f, new Color(1f, 0.85f, 0.4f));
            spriteRenderer.sortingOrder = 50;

            return fairy;
        }
    }
}
