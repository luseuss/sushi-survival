using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Pickups;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 잡몹 지속 스폰러. 런 경과 시간에 따라 스폰 간격은 짧아지고 한 번에 나오는 마릿수는
    /// 늘어나서, 초반엔 여유롭다가 보스전 무렵엔 화면이 가득 찬다(SpawnRampLogic).
    /// 시간이 정해진 등장(중형몹·몰려오는 웨이브)은 WaveDirector가 따로 맡는다.
    /// 캐릭터 선택 화면 동안에는 스폰하지 않는다.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Tooltip("잡몹 종류별 풀. 매 스폰마다 무작위로 하나를 고른다.")]
        [SerializeField] private GameObjectPool[] enemyPools;
        [SerializeField] private XPGemPoolSet xpGemPools;
        [SerializeField] private float spawnRadius = 10f;

        [Header("시간에 따른 스폰 증가")]
        [Tooltip("런 시작 때의 스폰 간격(초).")]
        [SerializeField] private float spawnInterval = 1.5f;
        [Tooltip("증가가 끝난 뒤의 스폰 간격(초).")]
        [SerializeField] private float endSpawnInterval = 0.6f;
        [Tooltip("런 시작 때 한 번에 스폰하는 마릿수.")]
        [SerializeField] private int startBatchSize = 1;
        [Tooltip("증가가 끝난 뒤 한 번에 스폰하는 마릿수.")]
        [SerializeField] private int endBatchSize = 2;
        [Tooltip("시작값에서 끝값까지 변하는 데 걸리는 시간(초). 보스 등장(300초) 조금 전으로 잡는다.")]
        [SerializeField] private float rampSeconds = 270f;
        [Tooltip("화면에 살아 있는 적이 이 수 이상이면 지속 스폰을 잠시 멈춘다(성능 보호).")]
        [SerializeField] private int maxAlive = 150;

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

        private float Elapsed => GameManager.Instance != null ? GameManager.Instance.ElapsedTime : 0f;

        private void Update()
        {
            if (!_spawning || _player == null) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            float elapsed = Elapsed;
            _timer = SpawnRampLogic.Interval(elapsed, spawnInterval, endSpawnInterval, rampSeconds);

            if (EnemyBase.AliveCount >= maxAlive) return;

            int batch = SpawnRampLogic.BatchSize(elapsed, startBatchSize, endBatchSize, rampSeconds);
            for (int i = 0; i < batch; i++)
                SpawnOne();
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
