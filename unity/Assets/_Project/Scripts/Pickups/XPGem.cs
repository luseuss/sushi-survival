using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Player;

namespace SushiSurvival.Pickups
{
    public class XPGem : MonoBehaviour
    {
        [Tooltip("플레이어를 찾지 못했을 때 쓰는 예비 반경.")]
        [SerializeField] private float fallbackPickupRadius = 0.5f;
        [SerializeField] private float xpValue = 1f; // 슬라이스1: 기본(흰색) 등급 고정값

        private GameObjectPool _selfPool;
        private Transform _player;
        private PlayerStats _playerStats;

        private void Awake()
        {
            // GameObjectPool.CreateInstance가 Instantiate(prefab, transform)으로
            // 생성하므로, 부모를 타고 올라가면 항상 자기 풀을 찾을 수 있다.
            _selfPool = GetComponentInParent<GameObjectPool>();
        }

        private void OnEnable()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                _player = null;
                _playerStats = null;
                return;
            }

            _player = playerObj.transform;
            _playerStats = playerObj.GetComponent<PlayerStats>();
        }

        private void Update()
        {
            if (_player == null) return;

            float radius = _playerStats != null
                ? _playerStats.GetValue(StatType.MagnetRange)
                : fallbackPickupRadius;

            if (!PickupUtility.IsWithinPickupRadius(transform.position, _player.position, radius)) return;

            GameManager.Instance.AddExperience(xpValue);

            if (_selfPool == null)
            {
                Debug.LogError($"{name}: selfPool을 찾지 못해 풀로 반환하지 못하고 파괴합니다.");
                Destroy(gameObject);
            }
            else
            {
                _selfPool.Release(gameObject);
            }
        }
    }
}
