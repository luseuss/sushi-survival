using UnityEngine;
using UnityEngine.Video;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 이 오브젝트의 사각형 안에 영상을 재생한다(시작 버튼 로고 등). 사각형의 위치와 크기를 에디터에서 바꾸면
    /// 영상이 그 자리에 그려진다. 앞 영상을 한 번 재생한 뒤 이어서 반복 영상을 계속 튼다. 반복 영상은 앞 영상이
    /// 도는 동안 미리 준비해 두어 이어지는 자리에서 끊기지 않는다. 영상이 준비되기 전이나 재생에 실패하면 아래 화면이 보인다.
    /// 둘 중 하나만 넣어도 된다(반복 영상만 넣으면 처음부터 반복).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class LoopingVideoSlot : MonoBehaviour
    {
        [Tooltip("먼저 한 번만 재생하는 영상. 비워두면 바로 반복 영상으로 시작한다.")]
        [SerializeField] private VideoClip introClip;
        [Tooltip("그 뒤로 계속 반복하는 영상.")]
        [SerializeField] private VideoClip loopClip;

        private void Start()
        {
            // 반복 영상을 먼저 만들어 아래에 깔고(준비만 해 둔다), 앞 영상을 그 위에 얹는다.
            VideoSurface loop = loopClip != null ? VideoSurface.Create(transform, loopClip, loop: true, "LoopVideo") : null;

            if (introClip == null)
            {
                if (loop != null) loop.Play();
                return;
            }

            VideoSurface intro = VideoSurface.Create(transform, introClip, loop: false, "IntroVideo");

            if (loop != null)
            {
                // 앞 영상이 끝나면 반복 영상을 시작하고, 반복 영상의 첫 프레임이 나온 뒤에 앞 영상을 치운다.
                intro.Finished += () => loop.Play();
                loop.Shown += intro.StopAndHide;
            }

            intro.Play();
        }
    }
}
