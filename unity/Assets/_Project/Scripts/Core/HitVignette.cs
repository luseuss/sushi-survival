using UnityEngine;
using UnityEngine.UI;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 플레이어가 맞을 때 화면 가장자리를 붉게 번쩍이는 전체 화면 효과. 씬에 배치하지 않고 JuiceDirector가 처음
    /// 필요할 때 만든다. 그림은 코드로 칠한 가장자리 그라데이션 하나이고, 화면 크기에 맞춰 늘어난다.
    /// 시간은 실시간(unscaled)을 쓴다 — 맞는 순간 히트스톱이 걸려 timeScale이 0이어도 효과는 계속 줄어야 한다.
    /// </summary>
    public class HitVignette : MonoBehaviour
    {
        private const int TextureSize = 128;
        private const int SortingOrder = 900;

        // 맞을 때마다 텍스처를 새로 칠하지 않도록 모두가 공유한다.
        private static Sprite _sprite;

        private Image _image;
        private Color _color;
        private float _peakAlpha;
        private float _duration;
        private float _age;
        private bool _playing;

        /// <summary>전체 화면 오버레이를 만들어 돌려준다. 처음에는 보이지 않는다.</summary>
        public static HitVignette Create(Transform parent)
        {
            var go = new GameObject("HitVignette", typeof(Canvas), typeof(HitVignette));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            var imageObject = new GameObject("Image", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(go.transform, false);

            var rect = (RectTransform)imageObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var vignette = go.GetComponent<HitVignette>();
            vignette._image = imageObject.GetComponent<Image>();
            vignette._image.sprite = GetSprite();
            vignette._image.raycastTarget = false;
            vignette._image.enabled = false;
            return vignette;
        }

        /// <summary>효과를 처음부터 다시 시작한다. 연속으로 맞으면 그때마다 다시 붉어진다.</summary>
        public void Play(Color color, float peakAlpha, float duration)
        {
            _color = color;
            _peakAlpha = peakAlpha;
            _duration = Mathf.Max(0.05f, duration);
            _age = 0f;
            _playing = true;
            Apply(0f);
        }

        private void Update()
        {
            if (!_playing) return;

            _age += Time.unscaledDeltaTime;
            float t = _age / _duration;

            if (t >= 1f)
            {
                _playing = false;
                _image.enabled = false;
                return;
            }

            Apply(t);
        }

        private void Apply(float t)
        {
            Color color = _color;
            color.a = HitVignetteLogic.Alpha(t, _peakAlpha);
            _image.color = color;
            _image.enabled = true;
        }

        private static Sprite GetSprite()
        {
            if (_sprite != null) return _sprite;

            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            float half = (TextureSize - 1) * 0.5f;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float edge = HitVignetteLogic.EdgeIntensity((x - half) / half, (y - half) / half);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, edge));
                }
            }

            texture.Apply();
            _sprite = Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f), 100f);
            return _sprite;
        }
    }
}
