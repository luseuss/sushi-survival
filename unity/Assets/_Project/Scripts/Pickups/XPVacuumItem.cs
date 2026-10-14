using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Player;

namespace SushiSurvival.Pickups
{
    /// <summary>
    /// 필드에 떨어지는 경험치 흡입 아이템(자석). 먹으면 바닥에 떨어진 경험치가 전부 플레이어에게 빨려 온다.
    /// 몬스터가 죽을 때 MonsterData.xpVacuumDropChance 확률로 TrySpawn이 만든다. 그림은 Resources의
    /// 자석 스프라이트를 쓰고, 씬에 따로 배치하거나 풀을 연결할 필요가 없다.
    /// </summary>
    public class XPVacuumItem : MonoBehaviour
    {
        private const string SpriteResourcePath = "Items/XpVacuum";
        private const int SortingOrder = 20;

        // 화면에 보이는 크기(월드 단위). 원본 그림이 커서 이 크기에 맞춰 줄인다.
        private const float WorldSize = 0.55f;
        private const float FallbackPickupRadius = 0.6f;
        private const float BobHeight = 0.08f;
        private const float BobSpeed = 4f;
        private const float PulseAmount = 0.08f;

        /// <summary>필드에 동시에 떠 있을 수 있는 아이템 수. 너무 많이 쌓여 화면이 어지러워지는 걸 막는다.</summary>
        public const int MaxActive = 3;

        private static Sprite _sprite;

        public static int ActiveCount { get; private set; }

        private Transform _player;
        private PlayerStats _playerStats;
        private Vector3 _origin;
        private float _age;
        private float _baseScale;

        /// <summary>확률에 따라 position에 아이템을 만든다. 이미 상한만큼 떠 있거나 그림을 못 찾으면 만들지 않는다.</summary>
        public static bool TrySpawn(Vector3 position, float chance)
        {
            if (!XPVacuumLogic.ShouldDrop(chance, Random.value)) return false;
            if (!XPVacuumLogic.CanSpawn(ActiveCount, MaxActive)) return false;

            Sprite sprite = GetSprite();
            if (sprite == null) return false;

            var go = new GameObject("XPVacuumItem");
            go.transform.position = position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = SortingOrder;

            var item = go.AddComponent<XPVacuumItem>();
            item._baseScale = WorldSize / Mathf.Max(0.01f, sprite.bounds.size.x);
            go.transform.localScale = Vector3.one * item._baseScale;
            return true;
        }

        private static Sprite GetSprite()
        {
            if (_sprite == null)
                _sprite = Resources.Load<Sprite>(SpriteResourcePath);

            return _sprite;
        }

        private void OnEnable()
        {
            ActiveCount++;
            _origin = transform.position;

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            _player = playerObj != null ? playerObj.transform : null;
            _playerStats = playerObj != null ? playerObj.GetComponent<PlayerStats>() : null;
        }

        private void OnDisable() => ActiveCount = Mathf.Max(0, ActiveCount - 1);

        private void Update()
        {
            _age += Time.deltaTime;

            // 눈에 잘 띄도록 위아래로 둥실거리며 살짝 커졌다 작아진다.
            float wave = Mathf.Sin(_age * BobSpeed);
            transform.position = _origin + Vector3.up * (wave * BobHeight);
            transform.localScale = Vector3.one * (_baseScale * (1f + wave * PulseAmount));

            if (_player == null) return;

            float radius = _playerStats != null
                ? Mathf.Max(FallbackPickupRadius, _playerStats.GetValue(StatType.MagnetRange))
                : FallbackPickupRadius;

            if (!PickupUtility.IsWithinPickupRadius(transform.position, _player.position, radius)) return;

            XPGem.AttractAll();
            Destroy(gameObject);
        }
    }
}
