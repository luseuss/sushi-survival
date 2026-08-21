using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Pickups;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>
    /// 플레이어를 둘러싸는 링 위에 잡몹을 솟아나게 한다. 등장 이펙트가 끝나야
    /// 실제 잡몹이 나오므로, 솟아오르는 도중에 맞아 죽는 억울함이 없다.
    /// </summary>
    public class SummonPattern : MonoBehaviour
    {
        [Tooltip("등장 이펙트가 끝나고 잡몹이 나올 때까지의 시간(초). 0이면 이펙트 길이를 쓴다.")]
        [SerializeField] private float summonDelayOverride;

        private Transform _player;
        private GameObjectPool _mobPool;
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

        public void Fire(BossPhaseValues values)
        {
            if (_mobPool == null || _player == null)
            {
                Debug.LogError($"{name}: mobPool 또는 player가 주입되지 않아 소환할 수 없습니다.");
                return;
            }

            // 매번 링을 무작위로 돌린다. 같은 자리에만 솟아나면 외워져서 긴장이 사라진다.
            float startAngle = Random.Range(0f, Mathf.PI * 2f);
            List<Vector2> positions = SummonPlacement.GetPositions(
                _player.position, values.summonCount, values.summonRadius, startAngle);

            foreach (Vector2 position in positions)
                StartCoroutine(SummonAt(position));
        }

        private IEnumerator SummonAt(Vector2 position)
        {
            float delay = summonDelayOverride;

            if (_effectPool != null)
            {
                GameObject effect = _effectPool.Get(position, Quaternion.identity);

                if (delay <= 0f && effect.TryGetComponent<OneShotEffect>(out var oneShot))
                    delay = oneShot.Duration;
            }

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            GameObject mob = _mobPool.Get(position, Quaternion.identity);

            // 스포너를 거치지 않으므로 젬 풀을 여기서 직접 주입한다.
            // 빠뜨리면 이 잡몹이 죽을 때만 젬이 안 떨어진다.
            if (mob.TryGetComponent<EnemyBase>(out var enemy))
                enemy.SetXpGemPools(_gemPools);
        }
    }
}
