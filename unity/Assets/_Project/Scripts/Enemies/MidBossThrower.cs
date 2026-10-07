using System.Collections;
using UnityEngine;
using SushiSurvival.Player;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 중형몹의 공격 패턴 — 일정 간격으로 멈춰 서서 롤 몬스터를 플레이어 쪽으로 던진다.
    /// 던지기 직전 잠깐 멈추는 시간이 플레이어가 알아채는 신호이고, 던진 뒤에는 다시 쫓아온다.
    /// 던진 롤은 던진 순간의 플레이어 위치에 떨어지므로 날아오는 동안 움직이면 피할 수 있다.
    /// </summary>
    [RequireComponent(typeof(EnemyAI))]
    public class MidBossThrower : MonoBehaviour
    {
        [SerializeField] private ThrownRoll rollPrefab;

        [Header("타이밍")]
        [Tooltip("등장하고 처음 던지기까지의 시간(초).")]
        [SerializeField] private float firstThrowDelay = 2f;
        [Tooltip("한 번 던지고 다음에 던지기까지의 시간(초).")]
        [SerializeField] private float throwInterval = 5f;
        [Tooltip("던지기 전에 멈춰 서 있는 시간(초). 플레이어가 알아챌 신호.")]
        [SerializeField] private float windupSeconds = 0.6f;
        [Tooltip("롤이 날아가는 시간(초). 길수록 피하기 쉽다.")]
        [SerializeField] private float flightSeconds = 0.9f;

        [Header("사거리")]
        [Tooltip("플레이어가 이보다 가까우면 던지지 않고 그냥 쫓아가 몸으로 때린다.")]
        [SerializeField] private float minRange = 3f;
        [Tooltip("플레이어가 이보다 멀면 던지지 않는다. 화면 밖으로 던지지 않게 한다.")]
        [SerializeField] private float maxRange = 9f;

        [Header("피해")]
        [SerializeField] private float damage = 12f;
        [Tooltip("착지 지점에서 이 반경 안에 있으면 맞는다. 예고 마커의 크기와 같다.")]
        [SerializeField] private float landingRadius = 1f;

        private EnemyAI _ai;
        private PlayerHealth _player;
        private float _cooldown;
        private Coroutine _routine;

        private void Awake() => _ai = GetComponent<EnemyAI>();

        private void OnEnable()
        {
            // 풀에서 재사용되므로 이전 개체의 진행 상태를 지운다.
            _cooldown = firstThrowDelay;
            _routine = null;
        }

        private void OnDisable() => _routine = null;

        private void Update()
        {
            if (_routine != null || rollPrefab == null) return;

            if (_player == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                _player = playerObj != null ? playerObj.GetComponent<PlayerHealth>() : null;
                if (_player == null) return;
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            float distance = Vector2.Distance(transform.position, _player.transform.position);
            if (!MidBossThrowLogic.InThrowRange(distance, minRange, maxRange)) return;

            _routine = StartCoroutine(ThrowRoutine());
        }

        private IEnumerator ThrowRoutine()
        {
            _ai.MoveScale = 0f;
            yield return new WaitForSeconds(windupSeconds);

            // 던지는 순간의 위치로 목표를 정한다 — 멈춰 있던 동안 플레이어가 움직였을 수 있다.
            if (_player != null)
            {
                ThrownRoll roll = Instantiate(rollPrefab, transform.position, Quaternion.identity);
                roll.Initialize(transform.position, _player.transform.position,
                                damage, landingRadius, flightSeconds, _player);
            }

            _ai.MoveScale = 1f;
            _cooldown = throwInterval;
            _routine = null;
        }
    }
}
