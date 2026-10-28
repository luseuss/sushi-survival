using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 설명창의 두루마리 위 화면 — 왼쪽 4×4 칸 격자와 오른쪽 영상 박스·제목·설명. 칸에 마우스를 올리거나 클릭하면
    /// (또는 방향키로 옮기면) 오른쪽이 그 칸의 내용으로 바뀐다. 씬에 따로 배선하지 않고 AugmentGuidePanel이 런타임에 만든다.
    /// 영상이 없는 칸은 큰 아이콘(펫은 움직이는 그림)을 영상 자리에 대신 보여준다.
    /// 대화 중 일시정지될 수 있어 시간은 unscaled로 센다.
    /// </summary>
    public class GuideBoard : MonoBehaviour
    {
        private GuideLayout _layout;
        private Sprite _slotFrame;
        private Sprite _slotFrameSelected;

        private RectTransform _root;
        private CanvasGroup _group;

        private readonly List<Image> _slotFrames = new List<Image>();
        private readonly List<Image> _slotIcons = new List<Image>();
        private readonly List<GuideSlot> _slots = new List<GuideSlot>();

        private GameObject _videoFrameObject;
        private Image _fallbackIcon;
        private VideoSurface _video;
        private Text _title;
        private Text _body;

        private IReadOnlyList<GuideItem> _items = new List<GuideItem>();
        private int _selected = -1;
        private float _animClock;

        public RectTransform Root => _root;
        public CanvasGroup Group => _group;
        public int SelectedIndex => _selected;

        /// <param name="parent">두루마리(Window). 1920×1080 중앙 기준 좌표를 그대로 쓴다.</param>
        public static GuideBoard Create(Transform parent, Font font, Sprite slotFrame, Sprite slotFrameSelected,
                                        Sprite videoFrame, GuideLayout layout)
        {
            var go = new GameObject("GuideBoard", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            // 두루마리 바로 위, 탭·제목·닫기 버튼과 게임플레이 내용보다는 아래에 깔린다.
            rect.SetAsFirstSibling();

            GuideBoard board = go.AddComponent<GuideBoard>();
            board.Build(rect, font, slotFrame, slotFrameSelected, videoFrame, layout ?? new GuideLayout());
            return board;
        }

        private void Build(RectTransform root, Font font, Sprite slotFrame, Sprite slotFrameSelected, Sprite videoFrame,
                           GuideLayout layout)
        {
            _root = root;
            _layout = layout;
            _slotFrame = slotFrame;
            _slotFrameSelected = slotFrameSelected;
            _group = gameObject.AddComponent<CanvasGroup>();

            BuildSlots();
            BuildDetail(font, videoFrame);
        }

        private void BuildSlots()
        {
            Vector2 frameSize = _slotFrame != null ? _slotFrame.rect.size : new Vector2(160f, 150f);

            for (int i = 0; i < GuideGridLogic.Capacity; i++)
            {
                var slotObject = new GameObject("Slot_" + i, typeof(RectTransform), typeof(Image));
                slotObject.transform.SetParent(_root, false);

                var slotRect = (RectTransform)slotObject.transform;
                SetCentered(slotRect, GuideGridLogic.SlotCenter(i, _layout.firstSlotCenter, _layout.slotPitch,
                                                                GuideGridLogic.Columns), frameSize);

                var frame = slotObject.GetComponent<Image>();
                frame.sprite = _slotFrame;
                frame.raycastTarget = true;

                var slot = slotObject.AddComponent<GuideSlot>();
                slot.Index = i;
                slot.OnSelect = Select;

                var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(slotRect, false);
                SetCentered((RectTransform)iconObject.transform, Vector2.zero, _layout.slotIconSize);

                var icon = iconObject.GetComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.enabled = false;

                _slotFrames.Add(frame);
                _slotIcons.Add(icon);
                _slots.Add(slot);
            }
        }

        private void BuildDetail(Font font, Sprite videoFrame)
        {
            // 영상 틀 그림 + 그 안쪽의 영상 영역(검은 바탕 → 영상/대체 그림).
            Vector2 frameSize = videoFrame != null ? videoFrame.rect.size : _layout.videoInnerSize + new Vector2(52f, 36f);
            _videoFrameObject = new GameObject("VideoFrame", typeof(RectTransform), typeof(Image));
            _videoFrameObject.transform.SetParent(_root, false);
            SetCentered((RectTransform)_videoFrameObject.transform, _layout.videoFrameCenter, frameSize);

            var frameImage = _videoFrameObject.GetComponent<Image>();
            frameImage.sprite = videoFrame;
            frameImage.raycastTarget = false;

            // 영상 영역은 틀보다 먼저 그려져야 틀 테두리가 영상을 덮는다.
            var innerObject = new GameObject("VideoArea", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            innerObject.transform.SetParent(_root, false);
            innerObject.transform.SetSiblingIndex(_videoFrameObject.transform.GetSiblingIndex());
            SetCentered((RectTransform)innerObject.transform, _layout.videoFrameCenter, _layout.videoInnerSize);

            var backdrop = innerObject.GetComponent<Image>();
            backdrop.color = _layout.videoBackdropColor;
            backdrop.raycastTarget = false;

            _video = VideoSurface.CreateEmpty(innerObject.transform, loop: true, "GuideVideo");

            var fallbackObject = new GameObject("FallbackIcon", typeof(RectTransform), typeof(Image));
            fallbackObject.transform.SetParent(innerObject.transform, false);
            SetCentered((RectTransform)fallbackObject.transform, Vector2.zero,
                        new Vector2(_layout.videoInnerSize.y * 0.7f, _layout.videoInnerSize.y * 0.7f));
            _fallbackIcon = fallbackObject.GetComponent<Image>();
            _fallbackIcon.preserveAspect = true;
            _fallbackIcon.raycastTarget = false;
            _fallbackIcon.enabled = false;

            _title = CreateText("Title", font, _layout.titleCenter, _layout.titleSize, _layout.titleFontSize, false);
            _body = CreateText("Body", font, _layout.bodyCenter, _layout.bodySize, _layout.bodyFontSize, true);
        }

        private Text CreateText(string objectName, Font font, Vector2 center, Vector2 size, int fontSize, bool wrapTop)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(_root, false);
            SetCentered((RectTransform)go.transform, center, size);

            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = _layout.textColor;
            text.alignment = wrapTop ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            if (wrapTop)
            {
                // 설명이 길면 줄여서라도 두루마리 아래 가로대를 넘지 않게 한다.
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 16;
                text.resizeTextMaxSize = fontSize;
            }

            var outline = go.GetComponent<Outline>();
            outline.effectColor = _layout.textOutlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private static void SetCentered(RectTransform rect, Vector2 center, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
        }

        /// <summary>격자 탭: 칸을 채우고 첫 칸을 고른다. 영상 박스와 설명은 선택한 칸에 따라 바뀐다.</summary>
        public void ShowGrid(IReadOnlyList<GuideItem> items)
        {
            _items = items ?? new List<GuideItem>();

            for (int i = 0; i < _slots.Count; i++)
            {
                // 내용이 없는 칸은 틀째로 숨긴다(빈 칸을 보여주지 않는다).
                bool filled = i < _items.Count;
                _slotFrames[i].gameObject.SetActive(filled);
                _slotFrames[i].raycastTarget = filled;

                Sprite icon = filled ? _items[i].icon : null;
                if (icon == null && filled && _items[i].frames != null && _items[i].frames.Length > 0)
                    icon = _items[i].frames[0];

                _slotIcons[i].sprite = icon;
                _slotIcons[i].enabled = icon != null;
            }

            _title.gameObject.SetActive(true);
            _body.gameObject.SetActive(true);
            _selected = -1;
            Select(0);
        }

        /// <summary>격자 없이 영상 박스만 보인다(게임플레이 탭). 영상이 없으면 검은 바탕만 남는다.</summary>
        public void ShowVideoOnly(VideoClip clip)
        {
            _items = new List<GuideItem>();
            _selected = -1;

            for (int i = 0; i < _slots.Count; i++)
            {
                _slotFrames[i].gameObject.SetActive(false);
                _slotIcons[i].enabled = false;
            }

            _title.gameObject.SetActive(false);
            _body.gameObject.SetActive(false);
            _fallbackIcon.enabled = false;
            _video.ChangeClip(clip);
        }

        public void Select(int index)
        {
            if (index < 0 || index >= _items.Count || index == _selected) return;

            _selected = index;
            _animClock = 0f;

            for (int i = 0; i < _slots.Count; i++)
                _slotFrames[i].sprite = i == _selected && _slotFrameSelected != null ? _slotFrameSelected : _slotFrame;

            GuideItem item = _items[index];
            _title.text = item.title;
            _body.text = item.body;

            if (item.video != null)
            {
                _fallbackIcon.enabled = false;
                _video.ChangeClip(item.video);
            }
            else
            {
                // 영상이 없으면 영상 자리에 큰 그림을 보여준다.
                _video.ChangeClip(null);
                ShowFallback(item, 0f);
            }
        }

        public void Navigate(int dx, int dy)
        {
            int next = GuideGridLogic.Move(_selected, dx, dy, GuideGridLogic.Columns, GuideGridLogic.Rows, _items.Count);
            if (next >= 0) Select(next);
        }

        public void StopVideo()
        {
            if (_video != null) _video.ChangeClip(null);
        }

        private void Update()
        {
            if (_selected < 0 || _selected >= _items.Count) return;

            GuideItem item = _items[_selected];
            if (item.video != null || item.frames == null || item.frames.Length < 2) return;

            _animClock += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            ShowFallback(item, _animClock);
        }

        private void ShowFallback(GuideItem item, float time)
        {
            Sprite sprite = item.icon;
            if (item.frames != null && item.frames.Length > 0)
            {
                int index = GuideAnimLogic.FrameIndex(time, item.framesPerSecond, item.frames.Length);
                if (index >= 0 && item.frames[index] != null) sprite = item.frames[index];
            }

            _fallbackIcon.sprite = sprite;
            _fallbackIcon.enabled = sprite != null;
        }
    }
}
