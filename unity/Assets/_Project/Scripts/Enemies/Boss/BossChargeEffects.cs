using System.Collections;
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>
    /// 보스 돌진의 시각 연출(예고선·잔상·먼지·충격파)을 총괄한다. 새 아트 없이 전부 런타임으로
    /// 만든다. 판정과 이동은 모르고 BossController.ChargeRoutine이 단계마다 부르기만 한다.
    /// </summary>
    public class BossChargeEffects : MonoBehaviour
    {
        // 64픽셀 원판·링 스프라이트의 월드 크기(CircleTextureFactory의 PPU 100 기준).
        private const float DiscWorldSize = 0.64f;

        [Header("방향 확정")]
        [Tooltip("예고가 끝나기 이만큼 전에 돌진 방향을 확정하고 예고선을 고정한다. " +
                 "0이면 예고 종료 순간에 확정한다(연출 추가 전의 기존 동작).")]
        [SerializeField] private float chargeLockSeconds = 0.15f;

        [Header("예고선")]
        [SerializeField] private float lineWidth = 0.12f;
        [SerializeField] private Color lineColor = new Color(1f, 0.15f, 0.15f, 0.35f);
        [SerializeField] private Color lineLockedColor = new Color(1f, 0.2f, 0.2f, 0.8f);
        [Tooltip("고정되는 순간 선이 이 배율만큼 굵어진다.")]
        [SerializeField] private float lineLockedWidthScale = 1.5f;

        [Header("잔상")]
        [SerializeField] private float ghostInterval = 0.05f;
        [SerializeField] private Color ghostColor = new Color(1f, 0.35f, 0.35f, 0.5f);
        [SerializeField] private float ghostLifetime = 0.25f;

        [Header("먼지")]
        [SerializeField] private float dustInterval = 0.04f;
        [SerializeField] private Color dustColor = new Color(0.85f, 0.75f, 0.55f, 0.6f);
        [Tooltip("먼지 원판의 지름(월드 단위).")]
        [SerializeField] private float dustStartSize = 0.15f;
        [SerializeField] private float dustEndSize = 0.4f;
        [SerializeField] private float dustLifetime = 0.35f;
        [Tooltip("먼지가 발치에서 좌우로 흩어지는 폭.")]
        [SerializeField] private float dustSpread = 0.25f;
        [Tooltip("보스 위치에서 발치까지 내려가는 거리.")]
        [SerializeField] private float dustFootOffset = 0.3f;

        [Header("충격파 / 화면 흔들림")]
        [SerializeField] private Color shockwaveColor = new Color(1f, 0.85f, 0.85f, 0.8f);
        [SerializeField] private float shockwaveStartRadius = 0.3f;
        [SerializeField] private float shockwaveEndRadius = 2.2f;
        [SerializeField] private float shockwaveLifetime = 0.35f;
        [SerializeField] private float shakeMagnitude = 0.25f;
        [SerializeField] private float shakeDuration = 0.25f;

        public float ChargeLockSeconds => chargeLockSeconds;

        private SpriteRenderer _bossRenderer;
        private Transform _root;
        private SpriteRenderer _line;
        private Sprite _pixelSprite;
        private Sprite _discSprite;
        private Sprite _ringSprite;
        private ObjectPool<FadingSpriteEffect> _pool;
        private Coroutine _trailRoutine;
        private float _lineLength;
        private bool _lineLocked;

        private void Awake()
        {
            _bossRenderer = GetComponent<SpriteRenderer>();
            if (_bossRenderer == null) _bossRenderer = GetComponentInChildren<SpriteRenderer>();

            // 보스를 따라 움직이면 안 되므로 효과는 씬 루트 아래에 둔다.
            _root = new GameObject("ChargeEffectsRoot").transform;

            _pixelSprite = CreatePixelSprite();
            _discSprite = CircleTextureFactory.CreateSprite(64, 0f, Color.white);
            _ringSprite = CircleTextureFactory.CreateSprite(64, CircleTextureFactory.RingInnerRatio, Color.white);

            var lineObject = new GameObject("ChargeTelegraph");
            lineObject.transform.SetParent(_root, false);
            _line = lineObject.AddComponent<SpriteRenderer>();
            _line.sprite = _pixelSprite;
            lineObject.SetActive(false);

            _pool = new ObjectPool<FadingSpriteEffect>(
                factory: () => FadingSpriteEffect.Create(_root, effect => _pool.Release(effect)),
                onGet: effect => effect.gameObject.SetActive(true),
                onRelease: effect => effect.gameObject.SetActive(false));
        }

        private void OnDisable()
        {
            // 예고나 돌진 도중에 보스가 죽어도 선이 남거나 코루틴이 이어지지 않게 한다.
            EndTrail();
            HideTelegraph();
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
            DestroySprite(_pixelSprite);
            DestroySprite(_discSprite);
            DestroySprite(_ringSprite);
        }

        // ---- 예고선 ----

        public void BeginTelegraph(float length)
        {
            _lineLength = length;
            _lineLocked = false;
            ApplyLine(_line.transform.position, _line.transform.eulerAngles.z);
            _line.gameObject.SetActive(true);
        }

        /// <summary>선의 시작점·방향을 갱신한다. 고정된 뒤에는 무시한다. 방향 계산은 실제 돌진과 같은 함수를 쓴다.</summary>
        public void AimTelegraph(Vector2 from, Vector2 toward)
        {
            if (_lineLocked) return;

            Vector2 direction = BossAimLogic.ChargeDirection(from, toward, Vector2.right);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            ApplyLine(from, angle);
        }

        public void LockTelegraph()
        {
            _lineLocked = true;
            ApplyLine(_line.transform.position, _line.transform.eulerAngles.z);
        }

        private void ApplyLine(Vector3 position, float angleDegrees)
        {
            float width = _lineLocked ? lineWidth * lineLockedWidthScale : lineWidth;

            _line.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angleDegrees));
            _line.transform.localScale = new Vector3(_lineLength, width, 1f);
            _line.color = _lineLocked ? lineLockedColor : lineColor;
            SyncSorting(_line);
        }

        private void HideTelegraph()
        {
            if (_line != null) _line.gameObject.SetActive(false);
        }

        // ---- 돌진 중 ----

        /// <summary>예고선을 끄고 잔상·먼지를 뿌리기 시작한다.</summary>
        public void BeginTrail()
        {
            HideTelegraph();

            if (_trailRoutine == null)
                _trailRoutine = StartCoroutine(TrailRoutine());
        }

        public void EndTrail()
        {
            if (_trailRoutine == null) return;

            StopCoroutine(_trailRoutine);
            _trailRoutine = null;
        }

        private IEnumerator TrailRoutine()
        {
            float ghostTimer = 0f;
            float dustTimer = 0f;

            while (true)
            {
                ghostTimer -= Time.deltaTime;
                dustTimer -= Time.deltaTime;

                if (ghostTimer <= 0f)
                {
                    SpawnGhost();
                    ghostTimer += ghostInterval;
                }

                if (dustTimer <= 0f)
                {
                    SpawnDust();
                    dustTimer += dustInterval;
                }

                yield return null;
            }
        }

        private void SpawnGhost()
        {
            if (_bossRenderer == null || _bossRenderer.sprite == null) return;

            Transform boss = _bossRenderer.transform;
            float scale = Mathf.Abs(boss.lossyScale.x);

            _pool.Get().Play(_bossRenderer.sprite, boss.position, boss.eulerAngles.z, ghostColor,
                             scale, scale, ghostLifetime,
                             _bossRenderer.sortingLayerID, _bossRenderer.sortingOrder - 1, _bossRenderer.flipX);
        }

        private void SpawnDust()
        {
            Vector3 position = transform.position
                + new Vector3(Random.Range(-dustSpread, dustSpread), -dustFootOffset, 0f);

            _pool.Get().Play(_discSprite, position, 0f, dustColor,
                             dustStartSize / DiscWorldSize, dustEndSize / DiscWorldSize, dustLifetime,
                             SortingLayerId(), SortingOrder() - 1);
        }

        // ---- 정지 순간 ----

        /// <summary>돌진이 멈추는 지점에 충격파 링을 퍼뜨리고 화면을 흔든다. 링은 피해가 없는 시각 효과다.</summary>
        public void PlayImpact(Vector2 position)
        {
            _pool.Get().Play(_ringSprite, position, 0f, shockwaveColor,
                             shockwaveStartRadius * 2f / DiscWorldSize, shockwaveEndRadius * 2f / DiscWorldSize,
                             shockwaveLifetime, SortingLayerId(), SortingOrder() - 1);

            if (JuiceDirector.Instance != null)
                JuiceDirector.Instance.Shake(shakeMagnitude, shakeDuration);
        }

        // ---- 공통 ----

        private int SortingLayerId() => _bossRenderer != null ? _bossRenderer.sortingLayerID : 0;
        private int SortingOrder() => _bossRenderer != null ? _bossRenderer.sortingOrder : 0;

        private void SyncSorting(SpriteRenderer target)
        {
            target.sortingLayerID = SortingLayerId();
            target.sortingOrder = SortingOrder() - 1;
        }

        private static Sprite CreatePixelSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            // 피벗을 왼쪽 가운데에 두고 PPU 1로 만들어, 스케일 x가 곧 선의 길이가 되게 한다.
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite == null) return;

            Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}
