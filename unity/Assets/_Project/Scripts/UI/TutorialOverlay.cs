using System;
using UnityEngine;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 연습 플레이 화면 위에 얹는 안내 UI — 위쪽 안내 띠(단계 번호 + 글), 왼쪽 위 "건너뛰기", 끝났을 때의 "시작하기".
    /// 코드로 만들어서 씬에는 배선이 필요 없다. 레벨업·가위바위보 창이 떠서 시간이 멈춰도 보이도록 가장 위 캔버스에 둔다.
    /// </summary>
    public class TutorialOverlay : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.1f, 0.08f, 0.14f, 0.86f);
        private static readonly Color GoldColor = new Color(0.96f, 0.78f, 0.26f, 1f);

        private Font _font;
        private Sprite _buttonSprite;
        private Text _stepLabel;
        private Text _body;
        private RectTransform _banner;
        private bool _docksBottom;
        private GameObject _doneButton;
        private GameObject _skipButton;
        private GameObject _warningRoot;
        private Text _warningText;

        public static TutorialOverlay Create(Font font, Sprite buttonSprite, Action onSkip, Action onStart)
        {
            var go = new GameObject("TutorialOverlay");
            var overlay = go.AddComponent<TutorialOverlay>();
            overlay._font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            overlay._buttonSprite = buttonSprite;
            overlay.Build(onSkip, onStart);
            return overlay;
        }

        /// <summary>안내 띠를 갱신한다. number가 0이면 단계 번호를 숨긴다.</summary>
        public void SetStep(int number, int total, string text)
        {
            _stepLabel.text = number > 0 ? $"{number}/{total}" : string.Empty;
            _body.text = text;
        }

        /// <summary>
        /// 안내 띠의 자리. 평소에는 화면 위쪽이고, 레벨업 카드 팝업(제목과 카드가 위쪽에 있다)이 떠 있는 동안에는
        /// 아래쪽으로 내려서 팝업 글자와 겹치지 않게 한다.
        /// </summary>
        public void SetDock(bool bottom)
        {
            if (_docksBottom == bottom) return;
            _docksBottom = bottom;

            float anchorY = bottom ? 0f : 1f;
            _banner.anchorMin = new Vector2(0.5f, anchorY);
            _banner.anchorMax = new Vector2(0.5f, anchorY);
            _banner.pivot = new Vector2(0.5f, anchorY);
            _banner.anchoredPosition = new Vector2(0f, bottom ? 60f : -130f);
        }

        /// <summary>
        /// 화면 가운데에 경고창(글 + 확인 버튼)을 띄운다. 뒤 화면을 어둡게 가리고 클릭을 막는다.
        /// 확인을 누르면 창이 닫히고 onOk이 불린다.
        /// </summary>
        public void ShowWarning(string text, Action onOk)
        {
            _warningText.text = text;
            _warningOk = onOk;
            _warningRoot.transform.SetAsLastSibling();
            _warningRoot.SetActive(true);
        }

        public bool IsWarningOpen => _warningRoot != null && _warningRoot.activeSelf;

        private Action _warningOk;

        private void OnWarningConfirmed()
        {
            _warningRoot.SetActive(false);
            Action callback = _warningOk;
            _warningOk = null;
            callback?.Invoke();
        }

        /// <summary>끝났을 때: 건너뛰기 대신 "시작하기"를 보여 준다.</summary>
        public void ShowDone()
        {
            _doneButton.SetActive(true);
            _skipButton.SetActive(false);
        }

        private void Build(Action onSkip, Action onStart)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // HUD·레벨업 창 위, 장면 전환(32000) 아래
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();

            // 위쪽 안내 띠: 타이머(맨 위 가운데)와 겹치지 않게 조금 아래에 둔다.
            RectTransform banner = NewRect("Banner", transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                           new Vector2(0f, -130f), new Vector2(1500f, 150f));
            _banner = banner;
            AddImage(banner.gameObject, PanelColor, false);

            RectTransform label = NewRect("Step", banner, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                          new Vector2(20f, 0f), new Vector2(170f, 100f));
            _stepLabel = AddText(label.gameObject, 44, GoldColor, TextAnchor.MiddleCenter);

            RectTransform body = NewRect("Body", banner, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                         Vector2.zero, Vector2.zero);
            body.offsetMin = new Vector2(215f, 8f);
            body.offsetMax = new Vector2(-30f, -8f); // 번호 칸(왼쪽 20~190)과 글 사이를 비워 글자가 겹치지 않게 한다
            _body = AddText(body.gameObject, 33, Color.white, TextAnchor.MiddleLeft);

            // 왼쪽 위 건너뛰기
            RectTransform skip = NewRect("Skip", transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                                         new Vector2(24f, -24f), new Vector2(315f, 60f));
            skip.pivot = new Vector2(0f, 1f);
            MakeButton(skip.gameObject, "건너뛰기", 33, PanelColor, onSkip);
            _skipButton = skip.gameObject;

            // 끝났을 때 시작하기(처음에는 숨김)
            RectTransform start = NewRect("Start", transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                          new Vector2(0f, -330f), new Vector2(433f, 82f));
            MakeButton(start.gameObject, "시작하기", 44, GoldColor, onStart);
            _doneButton = start.gameObject;
            _doneButton.SetActive(false);

            BuildWarning();
        }

        // 경고창: 화면 전체를 덮는 어두운 막 위에 테두리 있는 상자, 글, 확인 버튼. 처음에는 숨겨 둔다.
        private void BuildWarning()
        {
            RectTransform dim = NewRect("Warning", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddImage(dim.gameObject, new Color(0f, 0f, 0f, 0.6f), true);
            _warningRoot = dim.gameObject;

            RectTransform border = NewRect("Border", dim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 420f));
            AddImage(border.gameObject, GoldColor, false);
            RectTransform box = NewRect("Box", border, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(988f, 408f));
            AddImage(box.gameObject, new Color(0.1f, 0.08f, 0.14f, 0.97f), false);

            RectTransform title = NewRect("Title", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(900f, 70f));
            AddText(title.gameObject, 55, GoldColor, TextAnchor.MiddleCenter).text = "잠깐!";

            RectTransform body = NewRect("Body", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 150f));
            _warningText = AddText(body.gameObject, 33, Color.white, TextAnchor.UpperCenter);

            RectTransform ok = NewRect("Ok", box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(380f, 72f));
            MakeButton(ok.gameObject, "확인", 44, GoldColor, OnWarningConfirmed);

            _warningRoot.SetActive(false);
        }

        private RectTransform NewRect(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                      Vector2 position, Vector2 size)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            if (anchorMin == anchorMax)
            {
                rect.pivot = anchorMin;
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }
            else
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            return rect;
        }

        private static Image AddImage(GameObject go, Color color, bool raycast)
        {
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private Text AddText(GameObject go, int size, Color color, TextAnchor anchor)
        {
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.1f;
            text.raycastTarget = false;
            return text;
        }

        // 게임의 기본 버튼 그림이 있으면 그것을 쓰고(글자는 흰색), 없으면 단색 버튼으로 만든다.
        private void MakeButton(GameObject go, string label, int size, Color fallbackBackground, Action onClick)
        {
            Image image = AddImage(go, _buttonSprite != null ? Color.white : fallbackBackground, true);
            if (_buttonSprite != null) image.sprite = _buttonSprite;
            Color textColor = Color.white;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());

            RectTransform textRect = NewRect("Label", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text text = AddText(textRect.gameObject, size, textColor, TextAnchor.MiddleCenter);
            text.text = label;
        }
    }
}
