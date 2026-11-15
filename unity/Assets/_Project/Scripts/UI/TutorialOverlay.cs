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
        private Text _stepLabel;
        private Text _body;
        private GameObject _doneButton;
        private GameObject _skipButton;

        public static TutorialOverlay Create(Font font, Action onSkip, Action onStart)
        {
            var go = new GameObject("TutorialOverlay");
            var overlay = go.AddComponent<TutorialOverlay>();
            overlay._font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            overlay.Build(onSkip, onStart);
            return overlay;
        }

        /// <summary>안내 띠를 갱신한다. number가 0이면 단계 번호를 숨긴다.</summary>
        public void SetStep(int number, int total, string text)
        {
            _stepLabel.text = number > 0 ? $"{number}/{total}" : string.Empty;
            _body.text = text;
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
                                           new Vector2(0f, -130f), new Vector2(1300f, 130f));
            AddImage(banner.gameObject, PanelColor, false);

            RectTransform label = NewRect("Step", banner, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                          new Vector2(100f, 0f), new Vector2(180f, 100f));
            _stepLabel = AddText(label.gameObject, 44, GoldColor, TextAnchor.MiddleCenter);

            RectTransform body = NewRect("Body", banner, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                         Vector2.zero, Vector2.zero);
            body.offsetMin = new Vector2(210f, 8f);
            body.offsetMax = new Vector2(-30f, -8f);
            _body = AddText(body.gameObject, 33, Color.white, TextAnchor.MiddleLeft);

            // 왼쪽 위 건너뛰기
            RectTransform skip = NewRect("Skip", transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                                         new Vector2(24f, -24f), new Vector2(240f, 64f));
            skip.pivot = new Vector2(0f, 1f);
            MakeButton(skip.gameObject, "건너뛰기", 33, PanelColor, Color.white, onSkip);
            _skipButton = skip.gameObject;

            // 끝났을 때 시작하기(처음에는 숨김)
            RectTransform start = NewRect("Start", transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                          new Vector2(0f, -330f), new Vector2(420f, 90f));
            MakeButton(start.gameObject, "시작하기", 44, GoldColor, new Color(0.23f, 0.16f, 0f, 1f), onStart);
            _doneButton = start.gameObject;
            _doneButton.SetActive(false);
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

        private void MakeButton(GameObject go, string label, int size, Color background, Color textColor, Action onClick)
        {
            Image image = AddImage(go, background, true);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());

            RectTransform textRect = NewRect("Label", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text text = AddText(textRect.gameObject, size, textColor, TextAnchor.MiddleCenter);
            text.text = label;
        }
    }
}
