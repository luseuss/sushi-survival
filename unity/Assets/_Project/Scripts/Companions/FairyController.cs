using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Player;

namespace SushiSurvival.Companions
{
    /// <summary>
    /// 요정 시스템 총괄 — 씬 오브젝트(캐릭터 프리팹에 붙이지 않는다). 요정 목록을 들고 소환·강화하며,
    /// 보스 씬으로 레벨 배열을 넘기고 복원할 수 있게 한다. 요정 수치는 WeaponData(레벨 1~4)에서 읽는다.
    /// </summary>
    public class FairyController : MonoBehaviour
    {
        [Tooltip("요정 레벨별 수치. damage/cooldown/range(사거리)/pierceCount를 쓴다.")]
        [SerializeField] private WeaponData fairyData;
        [Tooltip("요정 투사체 풀. 풀 하나당 GameObject 하나(같은 오브젝트에 풀을 둘 붙이지 말 것).")]
        [SerializeField] private GameObjectPool projectilePool;
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("요정 프리팹. 비워두면 노란 원 플레이스홀더를 런타임에 만든다(아트가 들어오면 프리팹으로 교체).")]
        [SerializeField] private Fairy fairyPrefab;
        [SerializeField] private int maxFairies = 3;
        [SerializeField] private FairyMotion motion = FairyMotion.Default;

        private readonly List<Fairy> _fairies = new List<Fairy>();
        private readonly List<int> _levels = new List<int>();
        private Transform _player;
        private PlayerStats _stats;

        public int Count => _fairies.Count;
        public int MaxCount => maxFairies;
        public int MaxLevel => fairyData != null && fairyData.levels != null ? fairyData.levels.Length : 1;

        /// <summary>요정마다의 현재 레벨(소환 순서). 호출마다 내부 리스트를 다시 채워 돌려주므로 보관하려면 복사한다.</summary>
        public IReadOnlyList<int> Levels
        {
            get
            {
                _levels.Clear();
                foreach (Fairy fairy in _fairies)
                    _levels.Add(fairy.Level);
                return _levels;
            }
        }

        public void SetPlayer(Transform player, PlayerStats stats)
        {
            _player = player;
            _stats = stats;

            foreach (Fairy fairy in _fairies)
                fairy.SetPlayer(player, stats);
        }

        /// <summary>새 요정(Lv1)을 소환한다. 가득 찼으면 false.</summary>
        public bool Summon()
        {
            if (_fairies.Count >= maxFairies) return false;
            if (fairyData == null)
            {
                Debug.LogError($"{name}: fairyData가 비어 있어 요정을 소환할 수 없습니다.");
                return false;
            }

            Fairy fairy = CreateFairy();
            _fairies.Add(fairy);
            fairy.Initialize(_player, _stats, fairyData, projectilePool, enemyLayer, motion,
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
        /// 기존 요정을 비우고 레벨 배열대로 다시 만든다(보스 씬 복원용). null이면 아무것도 하지 않는다.
        /// SetPlayer가 먼저 불려 있어야 요정이 플레이어 곁에서 시작한다.
        /// </summary>
        public void Restore(IReadOnlyList<int> levels)
        {
            if (levels == null) return;

            foreach (Fairy fairy in _fairies)
            {
                if (fairy != null) Destroy(fairy.gameObject);
            }
            _fairies.Clear();

            foreach (int level in levels)
            {
                if (!Summon()) break;

                _fairies[_fairies.Count - 1].SetLevel(level);
            }
        }

        public List<FairyChoice> BuildChoices() => FairyChoiceLogic.Build(Levels, maxFairies, MaxLevel);

        /// <summary>다음 레벨과의 수치 비교 문구. 최대 레벨이거나 없는 번호면 빈 문자열.</summary>
        public string DescribeUpgrade(int index)
        {
            if (fairyData == null || index < 0 || index >= _fairies.Count) return string.Empty;

            int level = _fairies[index].Level;
            if (level >= fairyData.levels.Length) return string.Empty;

            return UpgradeDescriptionLogic.DescribeWeaponUpgrade(fairyData.levels[level - 1], fairyData.levels[level]);
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < _fairies.Count; i++)
                _fairies[i].SetSlot(i, _fairies.Count);
        }

        private Fairy CreateFairy()
        {
            if (fairyPrefab != null)
                return Instantiate(fairyPrefab, transform);

            var go = new GameObject("Fairy (placeholder)");
            go.transform.SetParent(transform, false);

            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CircleTextureFactory.CreateSprite(24, 0f, new Color(1f, 0.85f, 0.4f));
            spriteRenderer.sortingOrder = 50;

            return go.AddComponent<Fairy>();
        }
    }
}
