using System.Collections;
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 히트스톱·화면흔들림·사망 파티클을 한 곳에서 조정한다. 여러 히트가
    /// 같은 프레임에 겹쳐도(계란 양산이 한 번에 여러 마리를 죽이는 경우 등)
    /// DurationExtension으로 합쳐서 한 번의 반응으로 보이게 한다.
    /// </summary>
    public class JuiceDirector : MonoBehaviour
    {
        public static JuiceDirector Instance { get; private set; }

        [Header("참조")]
        [SerializeField] private CameraFollow cameraFollow;
        [Tooltip("사망 파티클 풀.")]
        [SerializeField] private GameObjectPool deathBurstPool;

        [Header("히트스톱")]
        [SerializeField] private float playerHitStopDuration = 0.08f;
        [SerializeField] private float enemyDeathStopDuration = 0.03f;

        [Header("화면 흔들림")]
        [SerializeField] private float playerHitShakeMagnitude = 0.15f;
        [SerializeField] private float playerHitShakeDuration = 0.2f;
        [SerializeField] private float enemyDeathShakeMagnitude = 0.05f;
        [SerializeField] private float enemyDeathShakeDuration = 0.1f;

        private Coroutine _hitstopRoutine;
        private float _hitstopResumeScale = 1f;
        private float _hitstopRemaining;

        private Coroutine _shakeRoutine;
        private float _shakeRemaining;
        private float _shakeMagnitude;

        private void Awake() => Instance = this;

        public void PlayerHit()
        {
            TriggerHitstop(playerHitStopDuration);
            TriggerShake(playerHitShakeMagnitude, playerHitShakeDuration);
        }

        public void EnemyDied(Vector3 position)
        {
            TriggerHitstop(enemyDeathStopDuration);
            TriggerShake(enemyDeathShakeMagnitude, enemyDeathShakeDuration);

            if (deathBurstPool != null)
                deathBurstPool.Get(position, Quaternion.identity);
        }

        private void TriggerHitstop(float duration)
        {
            if (duration <= 0f) return;

            if (_hitstopRoutine == null)
            {
                // 시작 시점의 timeScale을 캡처한다 — 팝업(0)이나 보스 연출(0.3)
                // 중이었다면 그 값으로 복구해야 한다. 1을 하드코딩하면 안 된다.
                _hitstopResumeScale = Time.timeScale;
                _hitstopRemaining = 0f;
                _hitstopRoutine = StartCoroutine(HitstopRoutine());
            }

            _hitstopRemaining = DurationExtension.Extend(_hitstopRemaining, duration);
        }

        private IEnumerator HitstopRoutine()
        {
            Time.timeScale = 0f;

            while (_hitstopRemaining > 0f)
            {
                _hitstopRemaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = _hitstopResumeScale;
            _hitstopRoutine = null;
        }

        private void TriggerShake(float magnitude, float duration)
        {
            if (magnitude <= 0f || duration <= 0f) return;

            if (_shakeRoutine == null)
            {
                _shakeRemaining = 0f;
                _shakeMagnitude = 0f;
                _shakeRoutine = StartCoroutine(ShakeRoutine());
            }

            _shakeRemaining = DurationExtension.Extend(_shakeRemaining, duration);
            // 더 큰 진폭이 우선한다 — 늘어난 지속시간에 비해 진폭이 작으면 약해 보인다.
            _shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitude);
        }

        private IEnumerator ShakeRoutine()
        {
            while (_shakeRemaining > 0f)
            {
                _shakeRemaining -= Time.unscaledDeltaTime;

                float magnitude = CameraShakeLogic.GetMagnitude(_shakeRemaining, _shakeMagnitude);
                Vector2 offset = Random.insideUnitCircle * magnitude;

                if (cameraFollow != null)
                    cameraFollow.SetShakeOffset(offset);

                yield return null;
            }

            if (cameraFollow != null)
                cameraFollow.SetShakeOffset(Vector2.zero);

            _shakeRoutine = null;
        }
    }
}
