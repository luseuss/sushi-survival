using System;
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 적을 때렸을 때 머리 위로 떠오르는 월드 공간 숫자 하나. JuiceDirector의 풀에서 꺼내 쓴다.
    /// 배경이 복잡해도 읽히도록 뒤에 검은 그림자를 한 겹 깐다.
    /// 시간은 scaled를 쓴다 — 일시정지·팝업 중엔 멈추고, 히트스톱 동안 아주 잠깐 멈추는 건 타격감에 어울린다.
    /// </summary>
    [RequireComponent(typeof(TextMesh), typeof(MeshRenderer))]
    public class DamageNumberPopup : MonoBehaviour
    {
        private const int SortingOrder = 500;
        private const float ShadowOffsetRatio = 0.07f;
        private const float ShadowAlpha = 0.85f;

        // 동적 폰트에서 TextMesh가 실제로 쓰는 래스터 크기. 정적 폰트는 이 값을 무시한다.
        private const int DynamicFontSize = 64;

        private TextMesh _text;
        private TextMesh _shadow;
        private Vector3 _origin;
        private Color _color;
        private float _baseScale;
        private float _riseHeight;
        private float _em;
        private float _life;
        private float _age;
        private Action<DamageNumberPopup> _onFinished;

        /// <summary>
        /// 숫자를 만든다. targetHeight는 글자 한 칸(em)의 월드 높이다. TextMesh는 한 칸의 높이가
        /// 글꼴 크기 × characterSize × 0.1 월드 단위인데, 정적으로 임포트된 폰트(갈무리가 그렇다)는
        /// TextMesh.fontSize를 무시하고 폰트 원래 크기를 쓴다. 그래서 폰트 종류에 따라 기준 크기를 골라야
        /// 숫자가 의도한 크기로 나온다. 실제로 보이는 숫자 높이는 한 칸의 70% 남짓이다.
        /// </summary>
        public static DamageNumberPopup Create(Transform parent, Font font, float targetHeight,
                                               Action<DamageNumberPopup> onFinished)
        {
            var go = new GameObject("DamageNumber");
            go.transform.SetParent(parent, false);

            float nativeSize = font.dynamic ? DynamicFontSize : Mathf.Max(1, font.fontSize);
            float characterSize = targetHeight / (nativeSize * 0.1f);

            var popup = go.AddComponent<DamageNumberPopup>();

            // 그림자를 먼저 만들어야 본문이 위에 그려진다(정렬 순서로도 보장한다).
            popup._shadow = CreateLayer("Shadow", go.transform, font, characterSize, SortingOrder - 1);
            popup._text = go.GetComponent<TextMesh>();
            ConfigureText(popup._text, font, characterSize);
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            go.GetComponent<MeshRenderer>().sortingOrder = SortingOrder;

            popup._em = targetHeight;
            popup._onFinished = onFinished;
            go.SetActive(false);
            return popup;
        }

        private static TextMesh CreateLayer(string layerName, Transform parent, Font font, float characterSize,
                                            int sortingOrder)
        {
            var layer = new GameObject(layerName);
            layer.transform.SetParent(parent, false);

            var text = layer.AddComponent<TextMesh>();
            ConfigureText(text, font, characterSize);

            var renderer = layer.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = sortingOrder;
            return text;
        }

        private static void ConfigureText(TextMesh text, Font font, float characterSize)
        {
            text.font = font;
            // 정적 폰트에 fontSize를 지정하면 유니티가 매번 경고를 찍는다(색을 바꿀 때마다 반복).
            if (font.dynamic)
                text.fontSize = DynamicFontSize;

            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = characterSize;
        }

        public void Play(Vector3 position, string value, Color color, float scale, float life, float riseHeight)
        {
            _origin = position;
            _color = color;
            _baseScale = scale;
            _life = Mathf.Max(0.05f, life);
            _riseHeight = riseHeight;
            _age = 0f;

            _text.text = value;
            _shadow.text = value;

            // 그림자는 본문의 오른쪽 아래로 글자 높이의 일부만큼 비킨다(부모 스케일을 따라간다).
            float em = _em;
            _shadow.transform.localPosition = new Vector3(em * ShadowOffsetRatio, -em * ShadowOffsetRatio, 0f);

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
            transform.position = _origin + Vector3.up * (_riseHeight * DamageNumberLogic.Rise(t));
            transform.localScale = Vector3.one * (_baseScale * DamageNumberLogic.PopScale(t));

            float alpha = DamageNumberLogic.Alpha(t);

            Color color = _color;
            color.a = alpha;
            _text.color = color;

            _shadow.color = new Color(0f, 0f, 0f, alpha * ShadowAlpha);
        }
    }
}
