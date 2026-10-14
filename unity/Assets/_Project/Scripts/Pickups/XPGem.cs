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

        // 지금 필드에 떠 있는(활성) 젬 전체. 흡입 아이템이 FindObjectsOfType 없이 한 번에 훑는다.
        private static readonly HashSet<XPGem> Active = new HashSet<XPGem>();

        private GameObjectPool _selfPool;
        private Transform _player;
        private PlayerStats _playerStats;
        private bool _attracted;
        private float _attractTime;

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
        }

        private void OnEnable()
        {
            Active.Add(this);

            // 풀에서 재사용되므로 이전 개체의 빨려 오던 상태를 지운다.
            _attracted = false;
            _attractTime = 0f;

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

        private void Update()
        {
            if (_player == null) return;

            if (_attracted)
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

            if (!PickupUtility.IsWithinPickupRadius(transform.position, _player.position, radius)) return;

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
