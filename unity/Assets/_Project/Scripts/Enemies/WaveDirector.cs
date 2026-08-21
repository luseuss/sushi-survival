using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Pickups;

namespace SushiSurvival.Enemies
{
    [System.Serializable]
    public class WaveEvent
    {
        [Tooltip("런 시작 후 이 시각(초)에 발화한다. 2:00 = 120, 4:00 = 240")]
        public float timeSeconds;
        [Tooltip("스폰할 몬스터의 풀.")]
        public GameObjectPool pool;
        [Min(1)]
        public int count = 1;
    }

    /// <summary>
    /// 런 경과 시간을 보며 예약된 스폰 이벤트를 발화한다. 잡몹 지속 스폰은
    /// EnemySpawner가 따로 담당하고, 여기서는 시각이 정해진 등장만 다룬다.
    /// </summary>
    public class WaveDirector : MonoBehaviour
    {
        [SerializeField] private WaveEvent[] events;
        [SerializeField] private XPGemPoolSet xpGemPools;
        [Tooltip("플레이어로부터 이 거리의 링 위에 등장시킨다.")]
        [SerializeField] private float spawnRadius = 8f;

        private readonly List<float> _eventTimes = new List<float>();

        private Transform _player;
        private bool _running;
        // 0초 이벤트도 놓치지 않도록 음수에서 시작한다.
        private float _previousTime = -1f;

        public void StartTimeline(Transform player)
        {
            _player = player;
            _previousTime = -1f;
            _running = true;

            _eventTimes.Clear();
            foreach (var waveEvent in events)
                _eventTimes.Add(waveEvent.timeSeconds);

            WarnAboutUnreachableEvents();
        }

        public void StopTimeline() => _running = false;

        private void Update()
        {
            if (!_running || _player == null) return;

            var manager = GameManager.Instance;
            if (manager == null) return;

            float now = manager.ElapsedTime;
            List<int> due = WaveSchedule.GetDueIndices(_eventTimes, _previousTime, now);
            _previousTime = now;

            foreach (int index in due)
                SpawnEvent(events[index]);
        }

        private void SpawnEvent(WaveEvent waveEvent)
        {
            if (waveEvent.pool == null)
            {
                Debug.LogError($"{name}: {waveEvent.timeSeconds}초 이벤트의 pool이 비어 있습니다.");
                return;
            }

            for (int i = 0; i < waveEvent.count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 spawnPos = SpawnRingUtility.GetPositionOnRing(_player.position, spawnRadius, angle);
                GameObject spawned = waveEvent.pool.Get(spawnPos, Quaternion.identity);

                if (spawned.TryGetComponent<EnemyBase>(out var enemy))
                    enemy.SetXpGemPools(xpGemPools);
            }

            Debug.Log($"[WaveDirector] {waveEvent.timeSeconds}초 이벤트 발화 — {waveEvent.count}마리");
        }

        private void WarnAboutUnreachableEvents()
        {
            float bossTime = GameManager.Instance != null
                ? GameManager.Instance.BossSpawnTime : float.MaxValue;

            foreach (var waveEvent in events)
            {
                if (waveEvent.timeSeconds > bossTime)
                    Debug.LogWarning($"{name}: {waveEvent.timeSeconds}초 이벤트는 " +
                                     $"보스 등장({bossTime}초)보다 뒤라 발화되지 않습니다.");
            }
        }
    }
}
