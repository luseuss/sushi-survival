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

        [Header("데미지 숫자")]
        [Tooltip("숫자에 쓸 폰트. 비워두면 유니티 기본 폰트를 쓴다.")]
        [SerializeField] private Font damageFont;
        [Tooltip("숫자 글자 한 칸의 높이(월드 단위). 실제 숫자는 이것의 70% 남짓이다. 잡몹 스프라이트가 대략 0.5 정도다.")]
        [SerializeField] private float numberHeight = 0.45f;
        [Tooltip("막타(적이 죽는 타격)일 때의 크기 배율.")]
        [SerializeField] private float killNumberScale = 1.5f;
        [SerializeField] private float numberLifetime = 0.6f;
        [Tooltip("숫자가 떠오르는 높이(월드 단위).")]
        [SerializeField] private float numberRiseHeight = 0.8f;
        [Tooltip("겹쳐 뜨지 않도록 숫자를 좌우로 흩뿌리는 폭(월드 단위).")]
        [SerializeField] private float numberJitter = 0.3f;
        [Tooltip("동시에 띄울 수 있는 숫자 수. 광역 공격으로 한꺼번에 맞을 때 화면이 숫자로 덮이지 않게 한다.")]
        [SerializeField] private int maxNumbers = 40;
        [SerializeField] private Color numberColor = Color.white;
        [SerializeField] private Color killNumberColor = new Color(1f, 0.85f, 0.3f);

        [Header("히트스톱")]
        [SerializeField] private float playerHitStopDuration = 0.08f;
        [SerializeField] private float enemyDeathStopDuration = 0.03f;

        [Header("화면 흔들림")]
        [SerializeField] private float playerHitShakeMagnitude = 0.15f;
        [SerializeField] private float playerHitShakeDuration = 0.2f;
        [SerializeField] private float enemyDeathShakeMagnitude = 0.05f;
        [SerializeField] private float enemyDeathShakeDuration = 0.1f;

        [Header("보스 진입 진동")]
        [Tooltip("보스전 진입 대화 동안 화면이 떨리는 세기(월드 단위).")]
        [SerializeField] private float rumbleMagnitude = 0.12f;
        [Tooltip("떨림이 최대 세기에 이르기까지 걸리는 시간(초).")]
        [SerializeField] private float rumbleRampSeconds = 1.2f;

        private ObjectPool<DamageNumberPopup> _numberPool;
        private int _activeNumbers;

        private Coroutine _hitstopRoutine;
        private float _hitstopResumeScale = 1f;
        private float _hitstopRemaining;

        private Coroutine _rumbleRoutine;
        private Coroutine _shakeRoutine;
        private float _shakeRemaining;
        private float _shakeMagnitude;

        private void Awake() => Instance = this;

        /// <summary>
        /// 보스 진입 대화처럼 정지(timeScale 0) 중인 화면을 "쿠쿵" 하고 계속 떨리게 한다. 처음에는 약하게
        /// 시작해 점점 세진다. EndRumble을 부를 때까지 이어진다. 실시간(unscaled)으로 진행한다.
        /// </summary>
        public void BeginRumble()
        {
            if (_rumbleRoutine != null) return;
            _rumbleRoutine = StartCoroutine(RumbleRoutine());
        }

        public void EndRumble()
        {
            if (_rumbleRoutine == null) return;

            StopCoroutine(_rumbleRoutine);
            _rumbleRoutine = null;

            if (cameraFollow != null)
                cameraFollow.SetShakeOffset(Vector2.zero);
        }

        private IEnumerator RumbleRoutine()
        {
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.unscaledDeltaTime;

                float ramp = Mathf.Clamp01(elapsed / rumbleRampSeconds);
                if (cameraFollow != null)
                    cameraFollow.SetShakeOffset(Random.insideUnitCircle * (rumbleMagnitude * ramp));

                yield return null;
            }
        }

        public void PlayerHit()
        {
            TriggerHitstop(playerHitStopDuration);
            TriggerShake(playerHitShakeMagnitude, playerHitShakeDuration);
        }

        /// <summary>적이 맞을 때마다 머리 위에 데미지 숫자를 띄운다. 막타는 더 크고 노랗게 보여준다.</summary>
        public void EnemyHit(Vector3 position, float damage, bool killed)
        {
            if (_activeNumbers >= maxNumbers) return;

            if (_numberPool == null)
            {
                Font font = damageFont != null
                    ? damageFont
                    : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) return;

                _numberPool = new ObjectPool<DamageNumberPopup>(
                    factory: () => DamageNumberPopup.Create(transform, font, numberHeight, ReleaseNumber),
                    onGet: popup => popup.gameObject.SetActive(true),
                    onRelease: popup => popup.gameObject.SetActive(false));
            }

            _activeNumbers++;
            Vector3 jittered = position + new Vector3(Random.Range(-numberJitter, numberJitter), 0.3f, 0f);
            _numberPool.Get().Play(
                jittered,
                DamageNumberLogic.Format(damage),
                killed ? killNumberColor : numberColor,
                killed ? killNumberScale : 1f,
                numberLifetime,
                numberRiseHeight);
        }

        private void ReleaseNumber(DamageNumberPopup popup)
        {
            _activeNumbers = Mathf.Max(0, _activeNumbers - 1);
            _numberPool.Release(popup);
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
