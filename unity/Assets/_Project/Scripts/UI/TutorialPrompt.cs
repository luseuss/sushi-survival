using System;
using UnityEngine;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 메인화면에서 "게임 시작"을 눌렀을 때 뜨는 연습 플레이 안내 창 — "연습하기"와 "건너뛰기".
    /// 코드로 만들어서 씬 배선이 필요 없고, 뒤의 화면을 어둡게 가려 다른 버튼이 눌리지 않게 한다.
    /// </summary>
    public class TutorialPrompt : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.1f, 0.08f, 0.14f, 0.96f);
        private static readonly Color BorderColor = new Color(0.96f, 0.78f, 0.26f, 1f);

        private Font _font;
        private Sprite _buttonSprite;

        /// <summary>canvasRoot 맨 위에 안내 창을 띄운다. buttonSprite는 메인화면의 시작 버튼 그림을 빌려 쓴다.</summary>
        public static TutorialPrompt Show(Transform canvasRoot, Font font, Sprite buttonSprite, Action onPractice, Action onSkip)
        {
            var go = new GameObject("TutorialPrompt", typeof(RectTransform));
            go.transform.SetParent(canvasRoot, false);
            var prompt = go.AddComponent<TutorialPrompt>();
            prompt._font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            prompt._buttonSprite = buttonSprite;
            prompt.Build(onPractice, onSkip);
            return prompt;
        }

        private void Build(Action onPractice, Action onSkip)
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.SetAsLastSibling();

            // 뒤 화면을 어둡게 가리면서 클릭을 막는다.
            var dim = gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            dim.raycastTarget = true;

            RectTransform border = NewRect("Border", root, new Vector2(0.5f, 0.5f), new Vector2(1000f, 480f), Vector2.zero);
            border.gameObject.AddComponent<Image>().color = BorderColor;
            RectTransform box = NewRect("Box", border, new Vector2(0.5f, 0.5f), new Vector2(988f, 468f), Vector2.zero);
            box.gameObject.AddComponent<Image>().color = PanelColor;

            RectTransform title = NewRect("Title", box, new Vector2(0.5f, 1f), new Vector2(900f, 70f), new Vector2(0f, -60f));
            AddText(title.gameObject, "연습 플레이를 해 볼까요?", 55, BorderColor);

            RectTransform body = NewRect("Body", box, new Vector2(0.5f, 1f), new Vector2(880f, 150f), new Vector2(0f, -190f));
            AddText(body.gameObject, "이동, 레벨업, 와사비, 스킬을\n직접 해 보면서 익힐 수 있어요. (약 2분)", 33, Color.white);

            MakeButton(box, "연습하기", new Vector2(-230f, -350f), onPractice);
            MakeButton(box, "건너뛰기", new Vector2(230f, -350f), onSkip);
        }

        private void MakeButton(RectTransform parent, string label, Vector2 position, Action onClick)
        {
            RectTransform rect = NewRect(label, parent, new Vector2(0.5f, 1f), new Vector2(380f, 90f), position);
            var image = rect.gameObject.AddComponent<Image>();
            if (_buttonSprite != null)
            {
                image.sprite = _buttonSprite;
                image.type = Image.Type.Simple;
            }
            else
            {
                image.color = new Color(0.2f, 0.2f, 0.3f, 1f);
            }

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());

            RectTransform text = NewRect("Label", rect, new Vector2(0.5f, 0.5f), new Vector2(380f, 90f), Vector2.zero);
            AddText(text.gameObject, label, 44, Color.white);
        }

        private static RectTransform NewRect(string objectName, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private void AddText(GameObject go, string content, int size, Color color)
        {
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }
    }
}
