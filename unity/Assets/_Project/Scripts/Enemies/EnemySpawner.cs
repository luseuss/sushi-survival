using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Pickups;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 슬라이스 1 전용 단순 스폰러 — 웨이브 타임라인 없이 잡몹을 일정 주기로
    /// 플레이어 주변 링에서 반복 스폰한다. 타임라인 연동은 이후 슬라이스에서.
    /// 캐릭터 선택 화면 동안에는 스폰하지 않는다.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Tooltip("잡몹 종류별 풀. 매 스폰마다 무작위로 하나를 고른다.")]
        [SerializeField] private GameObjectPool[] enemyPools;
        [SerializeField] private XPGemPoolSet xpGemPools;
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private float spawnInterval = 1.5f;

        private Transform _player;
        private bool _spawning;
        private float _timer;

        public void StartSpawning(Transform player)
        {
            _player = player;
            _timer = spawnInterval;
            _spawning = true;
        }

        public void StopSpawning() => _spawning = false;

        private void Update()
        {
            if (!_spawning || _player == null) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            SpawnOne();
            _timer = spawnInterval;
        }

        private void SpawnOne()
        {
            if (enemyPools == null || enemyPools.Length == 0)
            {
                Debug.LogError($"{name}: enemyPools가 비어 있어 스폰할 수 없습니다.");
                return;
            }

            GameObjectPool pool = enemyPools[Random.Range(0, enemyPools.Length)];

            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 spawnPos = SpawnRingUtility.GetPositionOnRing(_player.position, spawnRadius, angle);
            GameObject enemyObj = pool.Get(spawnPos, Quaternion.identity);

            if (enemyObj.TryGetComponent<EnemyBase>(out var enemy))
                enemy.SetXpGemPools(xpGemPools);
        }
    }
}
