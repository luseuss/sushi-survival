using System;
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 적을 맞힌 자리에 잠깐 번쩍이고 사라지는 별 모양 스파크 하나. JuiceDirector의 풀에서 꺼내 쓴다.
    /// 그림은 코드로 그린 픽셀 별이라 별도 에셋이 없다. 시간은 scaled를 쓴다 — 히트스톱 동안 멈추는 게 타격감에 어울린다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HitSpark : MonoBehaviour
    {
        private const int TextureSize = 32;
        private const int SortingOrder = 450;

        // 맞을 때마다 32×32 텍스처를 새로 굽지 않도록 모두가 공유한다.
        private static Sprite _sprite;

        private SpriteRenderer _renderer;
        private Color _color;
        private float _size;
        private float _life;
        private float _age;
        private Action<HitSpark> _onFinished;

        public static HitSpark Create(Transform parent, Action<HitSpark> onFinished)
        {
            var go = new GameObject("HitSpark");
            go.transform.SetParent(parent, false);

            var spark = go.AddComponent<HitSpark>();
            spark._renderer = go.GetComponent<SpriteRenderer>();
            spark._renderer.sprite = GetSprite();
            spark._renderer.sortingOrder = SortingOrder;
            spark._onFinished = onFinished;
            go.SetActive(false);
            return spark;
        }

        /// <param name="size">스파크가 다 커졌을 때의 월드 크기(한 변).</param>
        public void Play(Vector3 position, Color color, float size, float life)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 90f));
            _color = color;
            _size = size;
            _life = Mathf.Max(0.03f, life);
            _age = 0f;
            Apply(0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = _age / _life;

            if (t >= 1f)
            {
                _onFinished?.Invoke(this);
                return;
            }

            Apply(t);
        }

        private void Apply(float t)
        {
            transform.localScale = Vector3.one * (_size * HitSparkLogic.Scale(t, 0.35f, 1f));

            Color color = _color;
            color.a = _color.a * HitSparkLogic.Alpha(t);
            _renderer.color = color;
        }

        private static Sprite GetSprite()
        {
            if (_sprite != null) return _sprite;

            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            float half = (TextureSize - 1) * 0.5f;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    bool inside = HitSparkLogic.InsideStar((x - half) / half, (y - half) / half);
                    texture.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            texture.Apply();

            // 한 변이 정확히 1 월드 단위가 되도록 PPU를 텍스처 크기에 맞춘다.
            _sprite = Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f), TextureSize);
            return _sprite;
        }
    }
}
