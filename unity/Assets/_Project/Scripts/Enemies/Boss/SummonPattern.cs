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

            if (_mobPool == null) yield break;

            GameObject mob = _mobPool.Get(position, Quaternion.identity);
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