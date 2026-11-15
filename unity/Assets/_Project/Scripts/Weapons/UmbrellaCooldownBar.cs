using UnityEngine;
using UnityEngine.UI;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 클릭 스킬(아델린 회전 우산, 카마리온 와사비 샷건)의 쿨타임 막대. 체력바(HudHealthBar)의 자식으로 바로 아래에 붙어서 체력바를 따라다니며,
    /// 체력바와 같은 그림(틀)을 더 작게 쓴다. 쿨타임 동안 차오르고, 다 차면 금색으로 반짝여 "지금 쓸 수 있다"를 알린다.
    /// 우산 무기가 코드로 만들고 없앤다(씬 배선 없음). 체력바가 숨으면(전투 중이 아닐 때) 함께 숨는다.
    /// </summary>
    public class UmbrellaCooldownBar : MonoBehaviour
    {
        // 체력바 그림의 채움 영역 비율(틀 45x11 안에 채움 41x7). 같은 모양으로 맞춘다.
        private const float FillWidthRatio = 41f / 45f;
        private const float FillHeightRatio = 7f / 11f;

        private static readonly Color FillColor = new Color(0.35f, 0.8f, 1f, 1f);
        private static readonly Color ReadyColor = new Color(1f, 0.85f, 0.25f, 1f);
        // 스킬이 켜져 있는 동안(막대가 줄어드는 동안)의 색.
        private static readonly Color ActiveColor = new Color(1f, 0.55f, 0.15f, 1f);

        private Image _fill;
        private RectTransform _rect;
        private float _ratio;
        private bool _ready;
        private bool _active;
        private float _readySince;

        /// <summary>
        /// hpBar 바로 아래(간격 gap 픽셀)에 hpBar의 scale배 크기로 만든다.
        /// 틀 그림이 없으면 만들지 않고 null을 돌려준다.
        /// </summary>
        public static UmbrellaCooldownBar Create(RectTransform hpBar, Sprite frame, Sprite fill, float scale, float gap)
        {
            if (hpBar == null || frame == null || fill == null) return null;

            var go = new GameObject("UmbrellaCooldownBar", typeof(RectTransform));
            go.transform.SetParent(hpBar, false);

            var bar = go.AddComponent<UmbrellaCooldownBar>();
            bar._rect = (RectTransform)go.transform;
            bar._rect.anchorMin = new Vector2(0.5f, 0f);
            bar._rect.anchorMax = new Vector2(0.5f, 0f);
            bar._rect.pivot = new Vector2(0.5f, 1f);
            bar._rect.anchoredPosition = new Vector2(0f, -gap);
            Vector2 size = hpBar.sizeDelta * Mathf.Max(0.1f, scale);
            bar._rect.sizeDelta = size;

            Image frameImage = NewImage("Frame", bar._rect, frame);
            Stretch(frameImage.rectTransform);

            bar._fill = NewImage("Fill", bar._rect, fill);
            RectTransform fillRect = bar._fill.rectTransform;
            fillRect.anchorMin = fillRect.anchorMax = new Vector2(0.5f, 0.5f);
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(size.x * FillWidthRatio, size.y * FillHeightRatio);
            bar._fill.type = Image.Type.Filled;
            bar._fill.fillMethod = Image.FillMethod.Horizontal;
            bar._fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            bar._fill.color = FillColor;

            return bar;
        }

        /// <summary>
        /// 스킬이 켜져 있는 동안 남은 시간 비율(1 → 0). 주황색으로 줄어든다. 쿨타임이 돌기 시작하면 SetFill로 바꾼다.
        /// </summary>
        public void SetActiveRatio(float ratio)
        {
            _active = true;
            _ready = false;
            _ratio = Mathf.Clamp01(ratio);
        }

        /// <summary>쿨타임으로 막대가 찬 정도(0~1). 1이면 쓸 수 있는 상태로 보고 반짝인다.</summary>
        public void SetFill(float ratio)
        {
            _active = false;
            bool nowReady = ratio >= 1f;
            if (nowReady && !_ready) _readySince = Time.unscaledTime;

            _ratio = Mathf.Clamp01(ratio);
            _ready = nowReady;
        }

        private static Image NewImage(string imageName, Transform parent, Sprite sprite)
        {
            var go = new GameObject(imageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false; // 막대가 클릭을 가로채 우산 발동을 막지 않게 한다.
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            if (_fill == null) return;

            _fill.fillAmount = _ratio;

            if (_active)
            {
                _fill.color = ActiveColor;
                _rect.localScale = Vector3.one;
                return;
            }

            if (!_ready)
            {
                _fill.color = FillColor;
                _rect.localScale = Vector3.one;
                return;
            }

            // 준비되면 금색으로 숨 쉬듯 반짝이고, 막 준비된 순간에는 한 번 크게 튄다.
            float t = Time.unscaledTime - _readySince;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 8f);
            float flash = Mathf.Clamp01(1f - t / 0.25f);
            _fill.color = Color.Lerp(ReadyColor, Color.white, pulse * 0.6f);
            _rect.localScale = Vector3.one * (1f + 0.3f * flash + 0.04f * pulse);
        }
    }
}
