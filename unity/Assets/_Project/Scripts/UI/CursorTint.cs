using UnityEngine;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 조준점(마우스 커서) 이미지에 색을 입혀 커서로 쓴다. 원본 PNG는 건드리지 않는다.
    /// 텍스처의 Read/Write가 켜져 있어야 한다.
    /// </summary>
    public class CursorTint : MonoBehaviour
    {
        [Tooltip("커서로 쓸 이미지(조준점). Read/Write가 켜져 있어야 한다.")]
        [SerializeField] private Texture2D cursorTexture;
        [Tooltip("입힐 색. 알파로 전체 투명도도 조절한다.")]
        [SerializeField] private Color tint = Color.white;
        [Tooltip("켜면 원본 색을 지우고 이 색으로 통째로 칠한다(원본 모양의 투명도만 유지). " +
                 "끄면 원본 색에 곱한다(흰색 원본일 때 자연스럽다).")]
        [SerializeField] private bool replaceColor = true;
        [Tooltip("클릭 기준점(픽셀). 비워 두면 이미지 정중앙이다.")]
        [SerializeField] private bool useCenterHotspot = true;
        [SerializeField] private Vector2 hotspot;

        private Texture2D _tinted;

        private void Start() => Apply();

        // 인스펙터에서 색을 바꾸면 플레이 중에도 바로 반영한다.
        private void OnValidate()
        {
            if (Application.isPlaying && cursorTexture != null)
                Apply();
        }

        private void Apply()
        {
            if (cursorTexture == null)
            {
                Debug.LogWarning($"{name}: cursorTexture가 비어 있어 커서 색을 바꿀 수 없습니다.");
                return;
            }

            if (!cursorTexture.isReadable)
            {
                Debug.LogError($"{cursorTexture.name}: Read/Write가 꺼져 있어 색을 입힐 수 없습니다. " +
                               "이미지 Import Settings에서 Read/Write를 켜세요.");
                return;
            }

            if (_tinted != null) Destroy(_tinted);

            Color[] pixels = cursorTexture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color p = pixels[i];
                pixels[i] = replaceColor
                    ? new Color(tint.r, tint.g, tint.b, p.a * tint.a)
                    : new Color(p.r * tint.r, p.g * tint.g, p.b * tint.b, p.a * tint.a);
            }

            _tinted = new Texture2D(cursorTexture.width, cursorTexture.height, TextureFormat.RGBA32, false);
            _tinted.SetPixels(pixels);
            _tinted.Apply();

            Vector2 point = useCenterHotspot
                ? new Vector2(_tinted.width * 0.5f, _tinted.height * 0.5f)
                : hotspot;

            Cursor.SetCursor(_tinted, point, CursorMode.Auto);
        }

        // OnDestroy에서 _tinted를 지우지 않는다. 씬이 바뀌어도 커서 설정은 남아 있어서,
        // 아직 커서로 쓰이는 텍스처를 지워 버리면 커서가 깨질 수 있다.
    }
}
