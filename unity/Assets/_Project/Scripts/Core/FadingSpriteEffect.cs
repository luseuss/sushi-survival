using System;
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 스프라이트 하나를 놓고, 커지거나 작아지며 투명해졌다가 스스로 풀에 반환하는 효과.
    /// 돌진 잔상·먼지·충격파 링이 공통으로 쓴다. Time.deltaTime을 쓰므로 timeScale이 0이면 함께 멈춘다.
    /// </summary>
    public class FadingSpriteEffect : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Action<FadingSpriteEffect> _release;

        private Color _color;
        private float _startScale;
        private float _endScale;
        private float _lifetime;
        private float _age;

        public static FadingSpriteEffect Create(Transform parent, Action<FadingSpriteEffect> release)
        {
            var go = new GameObject("FadingSpriteEffect");
            go.transform.SetParent(parent, false);

            var effect = go.AddComponent<FadingSpriteEffect>();
            effect._renderer = go.AddComponent<SpriteRenderer>();
            effect._release = release;
            return effect;
        }

        public void Play(Sprite sprite, Vector3 position, float rotationDegrees, Color color,
                         float startScale, float endScale, float lifetime,
                         int sortingLayerId, int sortingOrder, bool flipX = false)
        {
            _renderer.sprite = sprite;
            _renderer.flipX = flipX;
            _renderer.sortingLayerID = sortingLayerId;
            _renderer.sortingOrder = sortingOrder;

            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, rotationDegrees));

            _color = color;
            _startScale = startScale;
            _endScale = endScale;
            _lifetime = lifetime;
            _age = 0f;

            Apply(0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;

            if (_age >= _lifetime)
            {
                _release?.Invoke(this);
                return;
            }

            Apply(BossEntranceLogic.Progress(_age, _lifetime));
        }

        private void Apply(float progress)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, progress);

            Color color = _color;
            color.a = _color.a * (1f - progress);
            _renderer.color = color;
        }
    }
}
