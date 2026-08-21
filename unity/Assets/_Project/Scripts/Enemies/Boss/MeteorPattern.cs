using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Player;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>
    /// 메테오를 연발한다. 각 발은 자기가 생성되는 순간의 플레이어 위치를 잡으므로
    /// 산개 로직이 따로 필요 없다 — 플레이어가 움직이면 흩어지고 멈춰 있으면
    /// 한자리에 겹쳐 떨어진다.
    /// </summary>
    public class MeteorPattern : MonoBehaviour
    {
        private PlayerHealth _player;
        private GameObjectPool _meteorPool;

        /// <summary>
        /// 프리팹은 씬에만 있는 풀을 Inspector로 참조할 수 없다.
        /// BossDirector → BossController를 거쳐 런타임에 주입된다.
        /// </summary>
        public void SetDependencies(PlayerHealth player, GameObjectPool meteorPool)
        {
            _player = player;
            _meteorPool = meteorPool;
        }

        public void Fire(BossPhaseValues values)
        {
            if (_meteorPool == null)
            {
                Debug.LogError($"{name}: meteorPool이 주입되지 않아 메테오를 쏠 수 없습니다.");
                return;
            }

            if (_player == null)
            {
                Debug.LogError($"{name}: player가 주입되지 않아 낙하 지점을 정할 수 없습니다.");
                return;
            }

            StartCoroutine(FireVolley(values));
        }

        private IEnumerator FireVolley(BossPhaseValues values)
        {
            IReadOnlyList<float> delays = MeteorVolley.GetLaunchDelays(values.meteorCount, values.meteorSpacing);
            float previousDelay = 0f;

            foreach (float delay in delays)
            {
                float wait = delay - previousDelay;
                if (wait > 0f) yield return new WaitForSeconds(wait);
                previousDelay = delay;

                // 플레이어가 이미 죽어 사라졌으면 남은 발을 쏘지 않는다.
                if (_player == null) yield break;

                LaunchOne(values);
            }
        }

        private void LaunchOne(BossPhaseValues values)
        {
            Vector2 target = _player.transform.position;
            GameObject obj = _meteorPool.Get(target, Quaternion.identity);

            if (!obj.TryGetComponent<Meteor>(out var meteor))
            {
                Debug.LogError($"{obj.name}: Meteor 컴포넌트가 없습니다.");
                return;
            }

            meteor.Initialize(target, values.meteorDamage, values.meteorRadius,
                              values.meteorWarningTime, _player, _meteorPool);
        }
    }
}
