using System.Collections.Generic;
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

        [Header("흡입 (경험치 흡입 아이템)")]
        [Tooltip("빨려 오기 시작할 때의 속도(월드 단위/초).")]
        [SerializeField] private float attractStartSpeed = 8f;
        [Tooltip("빨려 오는 동안 초당 이만큼씩 빨라진다.")]
        [SerializeField] private float attractAcceleration = 30f;
        [Tooltip("빨려 오는 속도의 상한.")]
        [SerializeField] private float attractMaxSpeed = 25f;

        [Header("먹을 때 연출 (플레이어에게 빨려 들어감)")]
        [Tooltip("자석 범위에 들어온 뒤 날아가기 시작할 때의 속도(월드 단위/초).")]
        [SerializeField] private float collectStartSpeed = 6f;
        [Tooltip("날아가는 동안 초당 이만큼씩 빨라진다.")]
        [SerializeField] private float collectAcceleration = 45f;
        [SerializeField] private float collectMaxSpeed = 22f;
        [Tooltip("플레이어와 이 거리 안이 되면 먹은 것으로 친다.")]
        [SerializeField] private float collectDistance = 0.15f;
        [Tooltip("이보다 오래 날면 그 자리에서 먹은 것으로 친다(경험치가 늦게 들어오는 것을 막는다).")]
        [SerializeField] private float maxFlightSeconds = 0.5f;
        [Tooltip("닿는 순간의 크기 배율. 날아가며 점점 이 크기까지 작아진다.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float arrivalScale = 0.55f;

        // 지금 필드에 떠 있는(활성) 젬 전체. 흡입 아이템이 FindObjectsOfType 없이 한 번에 훑는다.
        private static readonly HashSet<XPGem> Active = new HashSet<XPGem>();

        private GameObjectPool _selfPool;
        private Transform _player;
        private PlayerStats _playerStats;
        private bool _attracted;
        private float _attractTime;
        private bool _collecting;
        private float _flightTime;
        private float _flightStartDistance;
        private Vector3 _baseScale = Vector3.one;

        /// <summary>필드에 있는 모든 젬을 플레이어에게 빨려 오게 한다. 가까이 닿으면 평소처럼 경험치가 들어온다.</summary>
        public static void AttractAll()
        {
            foreach (XPGem gem in Active)
                gem.StartAttract();
        }

        private void StartAttract()
        {
            _attracted = true;
            _attractTime = 0f;
        }

        private void Awake()
        {
            // GameObjectPool.CreateInstance가 Instantiate(prefab, transform)으로
            // 생성하므로, 부모를 타고 올라가면 항상 자기 풀을 찾을 수 있다.
            _selfPool = GetComponentInParent<GameObjectPool>();
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            Active.Add(this);

            // 풀에서 재사용되므로 이전 개체의 빨려 오던 상태를 지운다.
            _attracted = false;
            _attractTime = 0f;
            _collecting = false;
            _flightTime = 0f;
            transform.localScale = _baseScale;

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

        private void OnDisable() => Active.Remove(this);

        /// <summary>플레이어를 향해 한 걸음 날아간다. 도착했으면(또는 너무 오래 날았으면) true.</summary>
        private bool FlyToPlayer()
        {
            _flightTime += Time.deltaTime;

            float speed = XPVacuumLogic.AttractSpeed(_flightTime, collectStartSpeed, collectAcceleration, collectMaxSpeed);
            transform.position = Vector3.MoveTowards(transform.position, _player.position, speed * Time.deltaTime);

            float distance = Vector3.Distance(transform.position, _player.position);
            transform.localScale = _baseScale * XPFeedbackLogic.ShrinkScale(distance, _flightStartDistance, arrivalScale);

            return XPFeedbackLogic.ShouldCollect(distance, _flightTime, collectDistance, maxFlightSeconds);
        }

        private void Update()
        {
            if (_player == null) return;

            // 날아가며 먹히는 중이면 아래 비행이 움직임을 맡는다(이중으로 움직이지 않게).
            if (_attracted && !_collecting)
            {
                _attractTime += Time.deltaTime;
                float speed = XPVacuumLogic.AttractSpeed(
                    _attractTime, attractStartSpeed, attractAcceleration, attractMaxSpeed);
                transform.position = Vector3.MoveTowards(
                    transform.position, _player.position, speed * Time.deltaTime);
            }

            float radius = _playerStats != null
                ? _playerStats.GetValue(StatType.MagnetRange)
                : fallbackPickupRadius;

            // 자석 범위에 들어오면 그 자리에서 사라지지 않고 플레이어에게 빨려 들어간 뒤 경험치가 들어온다.
            if (!_collecting)
            {
                if (!PickupUtility.IsWithinPickupRadius(transform.position, _player.position, radius)) return;

                _collecting = true;
                _flightTime = 0f;
                _flightStartDistance = Vector3.Distance(transform.position, _player.position);
            }

            if (!FlyToPlayer()) return;

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
