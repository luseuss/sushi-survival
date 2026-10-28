using UnityEngine;
using UnityEngine.Video;

namespace SushiSurvival.UI
{
    /// <summary>설명창 격자의 한 칸 — 아이콘과 오른쪽에 보여줄 제목·설명·영상. 영상이 없으면 frames(움직이는 그림) 또는 icon을 대신 보여준다.</summary>
    public sealed class GuideItem
    {
        public string title;
        public string body;
        public Sprite icon;
        public Sprite[] frames;
        public float framesPerSecond = 8f;
        public VideoClip video;
    }

    /// <summary>
    /// 설명창 배치. 1920×1080 기준이고 두루마리 한가운데가 (0,0), 위쪽이 +y다(목업 좌표를 옮긴 값).
    /// 두루마리 그림이나 칸 틀이 바뀌면 여기 값만 고친다.
    /// </summary>
    [System.Serializable]
    public class GuideLayout
    {
        [Header("왼쪽 칸 격자 (4×4)")]
        [Tooltip("왼쪽 위 첫 칸의 중심.")]
        public Vector2 firstSlotCenter = new Vector2(-604f, 222f);
        [Tooltip("칸 중심 사이 간격(가로, 세로).")]
        public Vector2 slotPitch = new Vector2(223f, 182.5f);
        [Tooltip("칸 안 아이콘 크기.")]
        public Vector2 slotIconSize = new Vector2(112f, 112f);

        [Header("오른쪽 영상 박스")]
        [Tooltip("영상 틀 그림의 중심.")]
        public Vector2 videoFrameCenter = new Vector2(434f, 131f);
        [Tooltip("틀 안쪽 영상이 나오는 영역 크기.")]
        public Vector2 videoInnerSize = new Vector2(449f, 298f);
        [Tooltip("영상 영역 뒤에 깔리는 색(영상 준비 전·영상이 없을 때 보인다).")]
        public Color videoBackdropColor = new Color(0.1f, 0.07f, 0.05f, 1f);

        [Header("오른쪽 설명 글")]
        public Vector2 titleCenter = new Vector2(434f, -90f);
        public Vector2 titleSize = new Vector2(502f, 56f);
        public Vector2 bodyCenter = new Vector2(434f, -262f);
        public Vector2 bodySize = new Vector2(502f, 300f);
        [Tooltip("갈무리 픽셀 폰트는 11의 배수 크기가 가장 또렷하다.")]
        public int titleFontSize = 33;
        public int bodyFontSize = 22;
        public Color textColor = Color.white;
        [Tooltip("두루마리 위에서 흰 글자가 읽히도록 두르는 테두리 색.")]
        public Color textOutlineColor = new Color(0.3f, 0.16f, 0.07f, 1f);
    }
}
