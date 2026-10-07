using System.Collections;
using UnityEngine;
using SushiSurvival.Player;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 중형몹의 공격 패턴 — 체력이 임계 비율 이하로 떨어지면 딱 한 번 멈춰 서서 롤 몬스터를 플레이어 쪽으로 던진다.
    /// 던지기 직전 잠깐 멈추는 시간이 플레이어가 알아채는 신호이고, 던진 뒤에는 다시 쫓아온다.
    /// 던진 롤은 던진 순간의 플레이어 위치에 떨어지므로 날아오는 동안 움직이면 피할 수 있다.
    /// 던진 직후에는 마끼를 잃은 모습으로 변하는 애니메이션(Transform 트리거)이 한 번 재생되고 마지막 프레임에 머문다.
    /// </summary>
    [RequireComponent(typeof(EnemyAI))]
    [RequireComponent(typeof(EnemyBase))]
    public class MidBossThrower : MonoBehaviour
    {
        [SerializeField] private ThrownRoll rollPrefab;

        [Header("발동 조건")]
        [Tooltip("체력이 최대 체력의 이 비율 이하가 되면 한 번 던진다. 0.3 = 30%.")]
        [Range(0.01f, 1f)]
        [SerializeField] private float healthThreshold = 0.3f;

        [Header("타이밍")]
        [Tooltip("던지기 전에 멈춰 서 있는 시간(초). 플레이어가 알아챌 신호.")]
        [SerializeField] private float windupSeconds = 0.6f;
        [Tooltip("롤이 날아가는 시간(초). 길수록 피하기 쉽다.")]
        [SerializeField] private float flightSeconds = 0.9f;

        [Header("피해")]
        [SerializeField] private float damage = 12f;
        [Tooltip("착지 지점에서 이 반경 안에 있으면 맞는다. 예고 마커의 크기와 같다.")]
        [SerializeField] private float landingRadius = 1f;

        private EnemyAI _ai;
        private EnemyBase _enemy;
        private Animator _animator;
        private SpriteRenderer _renderer;
        private Sprite _originalSprite;
        private PlayerHealth _player;
        private Coroutine _routine;
        private bool _thrown;

        private static readonly int TransformHash = Animator.StringToHash("Transform");
        private static readonly int IdleHash = Animator.StringToHash("Idle");

        private void Awake()
        {
            _ai = GetComponent<EnemyAI>();
            _enemy = GetComponent<EnemyBase>();
            _renderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            if (_renderer != null) _originalSprite = _renderer.sprite;
        }

        private void OnEnable()
        {
            // 풀에서 재사용되므로 이전 개체의 진행 상태를 지운다.
            _thrown = false;
            if (_animator != null)
            {
                _animator.ResetTrigger(TransformHash);
                _animator.Play(IdleHash, 0, 0f);
            }

            if (_renderer != null && _originalSprite != null) _renderer.sprite = _originalSprite;
            _routine = null;
        }

        private void OnDisable() => _routine = null;

        private void Update()
        {
            if (_thrown || _routine != null || rollPrefab == null) return;

            if (!MidBossThrowLogic.HealthBelowThreshold(_enemy.CurrentHealth, _enemy.MaxHealth, healthThreshold))
                return;

            if (_player == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                _player = playerObj != null ? playerObj.GetComponent<PlayerHealth>() : null;
                if (_player == null) return;
            }

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

            _thrown = true;
            if (_animator != null) _animator.SetTrigger(TransformHash);
            _ai.MoveScale = 1f;
            _routine = null;
        }
    }
}
