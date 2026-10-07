using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Player;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 중형몹이 던진 롤 몬스터. 던진 순간의 플레이어 위치에 낙하 예고 마커를 띄우고, 그림은 포물선으로
    /// 날아가 거기에 떨어진다. 데미지는 착지 순간에 한 번만 들어가므로 날아오는 동안 벗어나면 피한다.
    ///
    /// 지금 날아가는 그림(body)은 롤 몬스터 아트가 없어 임시 박스다 — body의 스프라이트만 바꾸면 된다.
    /// </summary>
    public class ThrownRoll : MonoBehaviour
    {
        private const int MarkerTextureSize = 64;

        [Header("날아가는 그림 (지금은 임시 박스)")]
        [SerializeField] private Transform body;
        [Tooltip("포물선의 가장 높은 지점(월드 단위).")]
        [SerializeField] private float peakHeight = 1.5f;
        [Tooltip("날아가는 동안 그림이 도는 속도(초당 도).")]
        [SerializeField] private float spinDegreesPerSecond = 540f;

        [Header("낙하 예고 마커 (스프라이트는 런타임에 채워진다 — 비워두는 게 정상)")]
        [SerializeField] private SpriteRenderer markerRing;
        [Tooltip("채움 원판. 스케일이 0에서 1로 커지며 남은 시간을 표현한다.")]
        [SerializeField] private SpriteRenderer markerFill;
        [SerializeField] private Color markerColor = new Color(0.9f, 0.5f, 0.1f, 0.85f);
        [SerializeField] private Color fillColor = new Color(0.9f, 0.5f, 0.1f, 0.35f);

        // 던질 때마다 64×64 텍스처를 새로 굽지 않도록 모두가 공유한다.
        private static Sprite _ringSprite;
        private static Sprite _discSprite;

        private PlayerHealth _player;
        private Vector2 _start;
        private Vector2 _target;
        private float _damage;
        private float _radius;
        private float _flightSeconds;
        private float _markerFullScale;
        private float _timer;

        private void Awake() => EnsureSprites();

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

        public void Initialize(Vector2 start, Vector2 target, float damage, float radius,
                               float flightSeconds, PlayerHealth player)
        {
            _start = start;
            _target = target;
            _damage = damage;
            _radius = radius;
            _flightSeconds = Mathf.Max(0.01f, flightSeconds);
            _player = player;
            _timer = 0f;

            // 마커는 착지 지점에 고정한다. 그림은 월드 좌표로 따로 움직인다.
            transform.position = target;

            // CreateSprite가 PPU 100으로 만들므로 스프라이트 한 변은 size/100 유닛이다.
            _markerFullScale = radius * 2f / (MarkerTextureSize / 100f);
            if (markerRing != null) markerRing.transform.localScale = Vector3.one * _markerFullScale;
            if (markerFill != null) markerFill.transform.localScale = Vector3.zero;

            UpdateBody(0f);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float progress = Mathf.Clamp01(_timer / _flightSeconds);

            if (markerFill != null)
                markerFill.transform.localScale = Vector3.one * (_markerFullScale * progress);

            UpdateBody(progress);

            if (progress >= 1f)
                Land();
        }

        private void UpdateBody(float progress)
        {
            if (body == null) return;

            Vector2 ground = Vector2.Lerp(_start, _target, progress);
            float height = MidBossThrowLogic.ArcHeight(progress, peakHeight);
            body.position = new Vector3(ground.x, ground.y + height, body.position.z);
            body.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
        }

        private void Land()
        {
            if (_player != null && Vector2.Distance(_player.transform.position, _target) <= _radius)
                _player.TakeDamage(_damage);

            Destroy(gameObject);
        }
    }
}
