using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Player;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>
    /// 예고 마커를 띄우고, 불덩이가 하늘에서 떨어져 폭발한다. 데미지는 폭발
    /// 순간에 한 번만 들어간다.
    ///
    /// 플레이어만 때린다 — 소환된 잡몹까지 휩쓸면 보스가 자기 소환물을 지워
    /// 패턴 둘이 서로를 무효화한다.
    /// </summary>
    public class Meteor : MonoBehaviour
    {
        private const int MarkerTextureSize = 64;
        private const float FallHeight = 7f;

        [Header("마커 (스프라이트는 런타임에 채워진다 — 비워두는 게 정상)")]
        [SerializeField] private SpriteRenderer markerRing;
        [Tooltip("채움 원판. 스케일이 0에서 1로 커지며 남은 시간을 표현한다.")]
        [SerializeField] private SpriteRenderer markerFill;

        [Header("불덩이")]
        [Tooltip("낙하 프레임(0~3)을 반복 재생하는 오브젝트.")]
        [SerializeField] private GameObject fallingObject;
        [Tooltip("폭발 프레임(4~9)을 한 번 재생하는 오브젝트.")]
        [SerializeField] private GameObject explosionObject;
        [Tooltip("폭발 애니메이션 길이(초). 이 시간이 지나면 풀로 돌아간다.")]
        [SerializeField] private float explosionDuration = 0.5f;

        [Header("색")]
        [SerializeField] private Color markerColor = new Color(0.9f, 0.15f, 0.1f, 0.85f);
        [SerializeField] private Color fillColor = new Color(0.9f, 0.15f, 0.1f, 0.35f);

        // 모든 메테오가 공유한다. 발마다 64×64 텍스처를 새로 굽는 것은 낭비다.
        private static Sprite _ringSprite;
        private static Sprite _discSprite;

        private GameObjectPool _pool;
        private PlayerHealth _player;

        private Vector2 _impactPoint;
        private float _damage;
        private float _radius;
        private float _warningTime;
        private float _markerFullScale;
        private float _timer;
        private bool _exploded;

        private void Awake()
        {
            _pool = GetComponentInParent<GameObjectPool>();
            EnsureSprites();
        }

        private void EnsureSprites()
        {
            if (_ringSprite == null)
                _ringSprite = CircleTextureFactory.CreateSprite(
                    MarkerTextureSize, CircleTextureFactory.RingInnerRatio, Color.white);

            if (_discSprite == null)
                _discSprite = CircleTextureFactory.CreateSprite(MarkerTextureSize, 0f, Color.white);

            if (markerRing != null)
            {
                markerRing.sprite = _ringSprite;
                markerRing.color = markerColor;
            }

            if (markerFill != null)
            {
                markerFill.sprite = _discSprite;
                markerFill.color = fillColor;
            }
        }

        public void Initialize(Vector2 impactPoint, float damage, float radius,
                               float warningTime, PlayerHealth player, GameObjectPool pool)
        {
            _impactPoint = impactPoint;
            _damage = damage;
            _radius = radius;
            _warningTime = Mathf.Max(0.01f, warningTime);
            _player = player;
            if (pool != null) _pool = pool;

            _timer = 0f;
            _exploded = false;

            transform.position = impactPoint;

            SetMarkerScale();

            if (markerRing != null) markerRing.gameObject.SetActive(true);
            if (markerFill != null) markerFill.gameObject.SetActive(true);
            if (fallingObject != null) fallingObject.SetActive(true);
            if (explosionObject != null) explosionObject.SetActive(false);
        }

        /// <summary>
        /// 마커는 폭발 반경과 정확히 같은 크기여야 한다. 어긋나면 표시된 곳
        /// 밖에서 맞거나 안에서 안 맞아 억울해진다.
        /// </summary>
        private void SetMarkerScale()
        {
            // CreateSprite가 PPU 100으로 만들므로 스프라이트 한 변은 size/100 유닛이다.
            float spriteWorldSize = MarkerTextureSize / 100f;
            _markerFullScale = _radius * 2f / spriteWorldSize;

            if (markerRing != null) markerRing.transform.localScale = Vector3.one * _markerFullScale;
            if (markerFill != null) markerFill.transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            if (!_exploded)
            {
                TickWarning();
                return;
            }

            if (_timer >= _warningTime + explosionDuration)
                Despawn();
        }

        private void TickWarning()
        {
            float progress = Mathf.Clamp01(_timer / _warningTime);

            if (markerFill != null)
                markerFill.transform.localScale = Vector3.one * (_markerFullScale * progress);

            if (fallingObject != null)
            {
                // 위에서 낙하 지점까지 직선으로 내려온다.
                fallingObject.transform.position =
                    Vector2.Lerp(_impactPoint + Vector2.up * FallHeight, _impactPoint, progress);
            }

            if (progress >= 1f)
                Explode();
        }

        private void Explode()
        {
            _exploded = true;

            if (markerRing != null) markerRing.gameObject.SetActive(false);
            if (markerFill != null) markerFill.gameObject.SetActive(false);
            if (fallingObject != null) fallingObject.SetActive(false);
            if (explosionObject != null) explosionObject.SetActive(true);

            if (_player == null) return;

            if (Vector2.Distance(_player.transform.position, _impactPoint) <= _radius)
                _player.TakeDamage(_damage);
        }

        private void Despawn()
        {
            if (_pool != null)
                _pool.Release(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
