using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 마우스를 올리면 anchor(카드 그림)를 덮어서 영상이 재생되고, 떼면 원래 그림으로 돌아간다.
    /// CharacterSelectButton이 캐릭터 데이터에 영상이 있을 때만 런타임에 붙인다.
    /// </summary>
    public class HoverVideoPreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private VideoSurface _surface;

        /// <param name="anchor">영상이 덮을 이미지(카드 그림). 이 오브젝트의 자식으로 표면이 생긴다.</param>
        public void Init(Graphic anchor, VideoClip clip)
        {
            // 씬이 시작될 때 미리 준비해 두면 처음 올릴 때도 딜레이 없이 바로 재생된다.
            _surface = VideoSurface.Create(anchor.transform, clip, loop: true, "HoverVideo");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_surface != null) _surface.Play();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_surface != null) _surface.StopAndHide();
        }

        private void OnDisable()
        {
            if (_surface != null) _surface.StopAndHide();
        }
    }
}
