using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Pickups;

namespace SushiSurvival.Enemies.Boss
{
    public class SummonPattern : MonoBehaviour
    {
        [Tooltip("등장 이펙트가 끝나고 잡몹이 나올 때까지의 시간(초). 0이면 이펙트 길이를 쓴다.")]
        [SerializeField] private float summonDelayOverride;
        [Tooltip("캘리포니아롤을 소환할 때 쓰는 등장 이펙트 프리팹(OneShotEffect 포함). " +
                 "비워두면 다른 몹과 같은 이펙트 풀을 쓴다.")]
        [SerializeField] private GameObject californiaEffectPrefab;

        private Transform _player;
        private GameObjectPool _mobPool;
        private GameObjectPool _californiaPool;
        private GameObjectPool _midPool;
        private GameObjectPool _effectPool;
        private XPGemPoolSet _gemPools;

        public void SetDependencies(Transform playerTransform, GameObjectPool mobPool,
                                    GameObjectPool effectPool, XPGemPoolSet gemPools)
        {
            _player = playerTransform;
            _mobPool = mobPool;
            _effectPool = effectPool;
            _gemPools = gemPools;
        }

        /// <summary>체력 임계 소환 단계에서 쓸 캘리포니아롤·중형몹 풀. 일반 몹 풀은 SetDependencies의 mobPool을 쓴다.</summary>
        public void SetStagePools(GameObjectPool californiaPool, GameObjectPool midPool)
        {
            _californiaPool = californiaPool;
            _midPool = midPool;
        }

        /// <summary>
        /// 소환 한 단계를 발동한다. 일반·캘리·중형몹을 섞어서 플레이어를 둘러싼 링 위에 균등하게 놓는다
        /// (종류별로 묶어 두면 한쪽에 같은 몹이 몰린다). 풀이 비어 있는 종류는 그 몹만 건너뛴다.
        /// </summary>
        public void FireStage(BossSummonStage stage, float radius)
        {
            if (_mobPool == null || _player == null)
            {
                Debug.LogError($"{name}: mobPool 또는 player가 주입되지 않아 소환할 수 없습니다.");
                return;
            }

            var pools = new List<GameObjectPool>();
            AddPools(pools, _mobPool, stage.basicCount, "일반");
            AddPools(pools, _californiaPool, stage.californiaCount, "캘리포니아");
            AddPools(pools, _midPool, stage.midCount, "중형몹");

            for (int i = pools.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pools[i], pools[j]) = (pools[j], pools[i]);
            }

            float startAngle = Random.Range(0f, Mathf.PI * 2f);
            List<Vector2> positions = SummonPlacement.GetPositions(_player.position, pools.Count, radius, startAngle);

            for (int i = 0; i < positions.Count; i++)
                StartCoroutine(SummonAt(positions[i], pools[i]));
        }

        private void AddPools(List<GameObjectPool> list, GameObjectPool pool, int count, string label)
        {
            if (count <= 0) return;

            if (pool == null)
            {
                Debug.LogError($"{name}: {label} 몹 풀이 비어 있어 {count}마리를 건너뜁니다.");
                return;
            }

            for (int i = 0; i < count; i++)
                list.Add(pool);
        }

        private IEnumerator SummonAt(Vector2 position, GameObjectPool pool)
        {
            float delay = summonDelayOverride;

            // 캘리포니아롤은 전용 이펙트라 풀 없이 만든다 — OneShotEffect가 풀을 못 찾으면 스스로 Destroy한다.
            GameObject effect = null;
            if (pool != null && pool == _californiaPool && californiaEffectPrefab != null)
                effect = Instantiate(californiaEffectPrefab, position, Quaternion.identity);
            else if (_effectPool != null)
                effect = _effectPool.Get(position, Quaternion.identity);

            if (effect != null && delay <= 0f && effect.TryGetComponent<OneShotEffect>(out var oneShot))
                delay = oneShot.Duration;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (pool == null) yield break;

            GameObject mob = pool.Get(position, Quaternion.identity);
            if (mob == null) yield break;

            if (mob.TryGetComponent<EnemyBase>(out var enemy))
            {
                if (_gemPools == null)
                {
                    _gemPools = FindObjectOfType<XPGemPoolSet>();
                }

                if (_gemPools != null)
                {
                    enemy.SetXpGemPools(_gemPools);
                }
                else
                {
                    Debug.LogWarning($"{name}: SummonPattern에 _gemPools가 없어 젬을 드랍할 수 없습니다.");
                }
            }
        }
    }
}