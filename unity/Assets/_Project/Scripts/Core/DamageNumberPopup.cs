using System;
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 적을 때렸을 때 머리 위로 떠오르는 월드 공간 숫자 하나. JuiceDirector의 풀에서 꺼내 쓴다.
    /// 시간은 scaled를 쓴다 — 일시정지·팝업 중엔 멈추고, 히트스톱 동안 아주 잠깐 멈추는 건 타격감에 어울린다.
    /// </summary>
    [RequireComponent(typeof(TextMesh), typeof(MeshRenderer))]
    public class DamageNumberPopup : MonoBehaviour
    {
        private const int SortingOrder = 500;

        private TextMesh _text;
        private Vector3 _origin;
        private Color _color;
        private float _baseScale;
        private float _riseHeight;
        private float _life;
        private float _age;
        private Action<DamageNumberPopup> _onFinished;

        /// <summary>
        /// 숫자를 만든다. targetHeight는 글자 한 칸(em)의 월드 높이다. TextMesh는 한 칸의 높이가
        /// fontSize × characterSize × 0.1 월드 단위라서 그 관계로 characterSize를 정한다.
        /// 숫자 자체는 한 칸의 70% 남짓이라 실제로 보이는 높이는 그보다 조금 작다.
        /// </summary>
        public static DamageNumberPopup Create(Transform parent, Font font, float targetHeight,
                                               Action<DamageNumberPopup> onFinished)
        {
            var go = new GameObject("DamageNumber");
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMesh>();
            var renderer = go.GetComponent<MeshRenderer>();

            text.font = font;
            text.fontSize = 64;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = targetHeight / (text.fontSize * 0.1f);
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = SortingOrder;

            var popup = go.AddComponent<DamageNumberPopup>();
            popup._text = text;
            popup._onFinished = onFinished;
            go.SetActive(false);
            return popup;
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

            Color color = _color;
            color.a = DamageNumberLogic.Alpha(t);
            _text.color = color;
        }
    }
}
