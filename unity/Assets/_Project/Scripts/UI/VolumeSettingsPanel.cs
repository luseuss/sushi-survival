using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Audio;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 일시정지(설정) 창에 전체·배경음악·효과음 볼륨 슬라이더를 만들어 AudioManager와 이어 준다.
    /// 슬라이더는 코드로 만들어서 씬에는 이 컴포넌트만 붙이면 된다. 값은 AudioManager가 저장하므로 다음 판에도 유지된다.
    /// </summary>
    public class VolumeSettingsPanel : MonoBehaviour
    {
        [Tooltip("첫 줄의 세로 위치(패널 중심 기준). 줄 사이 간격은 rowSpacing.")]
        [SerializeField] private float firstRowY = 40f;
        [SerializeField] private float rowSpacing = 60f;
        [SerializeField] private int fontSize = 33;

        private static readonly Color TrackColor = new Color(0.18f, 0.16f, 0.2f, 1f);
        private static readonly Color FillColor = new Color(0.96f, 0.78f, 0.3f, 1f);

        private Slider _master, _bgm, _sfx;
        private Text _masterValue, _bgmValue, _sfxValue;
        private Font _font;
        private bool _built;

        private void Awake() => Build();

        private void OnEnable()
        {
            Build();
            AudioManager.VolumeChanged += Refresh;
            Refresh();
        }

        private void OnDisable() => AudioManager.VolumeChanged -= Refresh;

        private void Build()
        {
            if (_built) return;
            _built = true;

            Text anyText = GetComponentInChildren<Text>(true);
            _font = anyText != null ? anyText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _master = BuildRow("전체", firstRowY, v => AudioManager.MasterVolume = v, out _masterValue);
            _bgm = BuildRow("배경음악", firstRowY - rowSpacing, v => AudioManager.BgmVolume = v, out _bgmValue);
            _sfx = BuildRow("효과음", firstRowY - rowSpacing * 2f, v => AudioManager.SfxVolume = v, out _sfxValue);
        }

        private void Refresh()
        {
            Set(_master, _masterValue, AudioManager.MasterVolume);
            Set(_bgm, _bgmValue, AudioManager.BgmVolume);
            Set(_sfx, _sfxValue, AudioManager.SfxVolume);
        }

        private static void Set(Slider slider, Text valueText, float volume)
        {
            if (slider != null) slider.SetValueWithoutNotify(volume);
            if (valueText != null) valueText.text = VolumeDisplayLogic.PercentText(volume);
        }

        private Slider BuildRow(string label, float y, UnityEngine.Events.UnityAction<float> onChanged, out Text valueText)
        {
            RectTransform row = NewRect(label + "_Row", transform);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(900f, 50f);
            row.anchoredPosition = new Vector2(0f, y);

            MakeText(label + "_Label", row, label, new Vector2(-330f, 0f), new Vector2(220f, 50f), TextAnchor.MiddleRight);
            valueText = MakeText(label + "_Value", row, "100%", new Vector2(350f, 0f), new Vector2(120f, 50f), TextAnchor.MiddleLeft);

            Slider slider = MakeSlider(label + "_Slider", row);
            slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        private Text MakeText(string objectName, Transform parent, string content, Vector2 pos, Vector2 size, TextAnchor anchor)
        {
            RectTransform rect = NewRect(objectName, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;

            Text text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }

        private static Slider MakeSlider(string objectName, Transform parent)
        {
            RectTransform root = NewRect(objectName, parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(420f, 28f);
            root.anchoredPosition = new Vector2(10f, 0f);

            Image track = root.gameObject.AddComponent<Image>();
            track.color = TrackColor;

            RectTransform fillArea = NewRect("Fill Area", root);
            Stretch(fillArea, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            RectTransform fill = NewRect("Fill", fillArea);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.sizeDelta = new Vector2(10f, 0f);
            fill.gameObject.AddComponent<Image>().color = FillColor;

            RectTransform handleArea = NewRect("Handle Slide Area", root);
            Stretch(handleArea, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            RectTransform handle = NewRect("Handle", handleArea);
            handle.anchorMin = new Vector2(0f, 0f);
            handle.anchorMax = new Vector2(0f, 1f);
            handle.sizeDelta = new Vector2(20f, 14f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;

            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            return slider;
        }

        private static RectTransform NewRect(string objectName, Transform parent)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
