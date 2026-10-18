using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 대화창 초상화/입상 이미지에 표정 연출을 입힌다 — 움직임·크기·기울기·색, 그리고 머리 옆 말풍선.
    /// 씬에 따로 배선하지 않고 필요할 때 붙인다(Play 한 줄이면 된다). 대화 중엔 timeScale이 0일 수 있어 unscaled 시간으로 진행한다.
    /// StandingAnimator가 같은 오브젝트에 있으면 그 움직임(등장 슬라이드·숨쉬기) 위에 얹어서 서로 덮어쓰지 않게 한다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ExpressionEffect : MonoBehaviour
    {
        private Image _image;
        private RectTransform _rect;
        private StandingAnimator _standing;
        private Text _bubble;
        private Outline _bubbleOutline;

        private ExpressionType _type;
        private float _clock;
        private bool _active;
        private Vector2 _basePosition;
        private Vector3 _baseScale = Vector3.one;
        private Color _baseColor = Color.white;
        private float _bubbleClock = float.MaxValue;

        /// <summary>
        /// target에 표정 연출을 재생한다. 평상시면 진행 중인 연출만 정리한다. target이 null이면 아무것도 안 한다.
        /// animateImage가 false면 그림은 움직이지 않고(위치·크기·기울기·색 그대로) 말풍선만 띄운다 — 대화창 안 작은 프로필용.
        /// </summary>
        public static void PlayOn(Image target, ExpressionType expression, Font font, bool showBubble, bool animateImage = true)
        {
            if (target == null) return;

            if (!target.TryGetComponent<ExpressionEffect>(out var effect))
                effect = target.gameObject.AddComponent<ExpressionEffect>();

            effect.Play(expression, font, showBubble, animateImage);
        }

        public void Play(ExpressionType expression, Font font, bool showBubble, bool animateImage = true)
        {
            Cache();
            ResetEffect();

            _type = expression;
            _clock = 0f;
            _bubbleClock = float.MaxValue;
            HideBubble();

            if (ExpressionLogic.Duration(expression) <= 0f) return;

            if (animateImage)
            {
                _active = true;
                _basePosition = _rect.anchoredPosition;
                _baseScale = _rect.localScale;
                _baseColor = _image.color;
            }

            if (showBubble && font != null)
                ShowBubble(expression, font);
        }

        private void Cache()
        {
            if (_image != null) return;

            _image = GetComponent<Image>();
            _rect = transform as RectTransform;
            _standing = GetComponent<StandingAnimator>();
        }

        private void OnDisable() => ResetEffect();

        // StandingAnimator가 Update에서 위치를 정한 뒤에 얹어야 하므로 LateUpdate에서 적용한다.
        private void LateUpdate()
        {
            if (_bubble != null && _bubbleClock < ExpressionLogic.BubbleDuration)
            {
                _bubbleClock += Time.unscaledDeltaTime;
                ApplyBubble();
            }

            if (!_active) return;

            _clock += Time.unscaledDeltaTime;
            if (_clock >= ExpressionLogic.Duration(_type))
            {
                ResetEffect();
                return;
            }

            ExpressionPose pose = ExpressionLogic.Evaluate(_type, _clock);

            // 움직임: StandingAnimator가 있으면 그 결과에 더하고, 없으면 처음 위치 기준으로 놓는다.
            Vector2 anchor = _standing != null ? _rect.anchoredPosition : _basePosition;
            _rect.anchoredPosition = anchor + pose.Offset;

            _rect.localScale = _baseScale * pose.Scale;
            _rect.localRotation = Quaternion.Euler(0f, 0f, pose.RotationDegrees);

            Color tinted = Color.Lerp(_baseColor, pose.Tint, pose.TintStrength);
            tinted.a = _image.color.a; // 알파는 StandingAnimator의 페이드가 맡는다.
            _image.color = tinted;
        }

        // 진행 중이던 연출을 처음 상태로 돌려놓는다(위치는 StandingAnimator가 있으면 그쪽이 다시 정한다).
        private void ResetEffect()
        {
            if (!_active || _rect == null) return;

            _active = false;
            if (_standing == null) _rect.anchoredPosition = _basePosition;
            _rect.localScale = _baseScale;
            _rect.localRotation = Quaternion.identity;

            Color restored = _baseColor;
            restored.a = _image.color.a;
            _image.color = restored;
        }

        private void ShowBubble(ExpressionType expression, Font font)
        {
            if (_bubble == null) CreateBubble();

            _bubble.font = font;
            _bubble.text = ExpressionLogic.Glyph(expression);
            _bubble.color = ExpressionLogic.GlyphColor(expression);

            float height = Mathf.Max(1f, _rect.rect.height);
            // 갈무리 픽셀 폰트는 11의 배수에서 가장 또렷하다.
            _bubble.fontSize = Mathf.Clamp(Mathf.RoundToInt(height * 0.13f / 11f) * 11, 22, 55);

            float inset = Mathf.Clamp(height * 0.1f, 12f, 50f);
            ((RectTransform)_bubble.transform).anchoredPosition = new Vector2(-inset, -inset);

            _bubbleClock = 0f;
            ApplyBubble();
        }

        private void CreateBubble()
        {
            var go = new GameObject("ExpressionBubble", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(120f, 70f);

            _bubble = go.AddComponent<Text>();
            _bubble.alignment = TextAnchor.MiddleCenter;
            _bubble.horizontalOverflow = HorizontalWrapMode.Overflow;
            _bubble.verticalOverflow = VerticalWrapMode.Overflow;
            _bubble.raycastTarget = false;

            _bubbleOutline = go.AddComponent<Outline>();
            _bubbleOutline.effectColor = Color.black;
            _bubbleOutline.effectDistance = new Vector2(2f, -2f);
        }

        private void ApplyBubble()
        {
            float alpha = ExpressionLogic.BubbleAlpha(_bubbleClock);
            float scale = ExpressionLogic.BubbleScale(_bubbleClock);

            Color color = ExpressionLogic.GlyphColor(_type);
            color.a = alpha;
            _bubble.color = color;
            _bubble.transform.localScale = Vector3.one * scale;

            Color outline = Color.black;
            outline.a = alpha;
            _bubbleOutline.effectColor = outline;

            _bubble.enabled = alpha > 0f;
        }

        private void HideBubble()
        {
            if (_bubble != null) _bubble.enabled = false;
        }
    }
}
