using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// UI Image에 프레임 그림을 차례로 보여 주는 애니메이션(GIF 대신). 켜질 때마다 처음부터 시작한다.
    /// 결과 화면처럼 시간이 멈춘 상태에서도 움직이도록 실시간(unscaled) 시간을 쓴다.
    /// 프레임이 비어 있으면 Image의 기존 그림을 그대로 둔다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UiFrameAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 25f;
        [SerializeField] private bool loop = true;

        private Image _image;
        private float _elapsed;

        private void Awake() => _image = GetComponent<Image>();

        private void OnEnable()
        {
            _elapsed = 0f;
            Apply();
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            Apply();
        }

        private void Apply()
        {
            if (_image == null || frames == null || frames.Length == 0) return;

            Sprite frame = frames[FrameAnimationLogic.FrameAt(_elapsed, framesPerSecond, frames.Length, loop)];
            if (frame != null && _image.sprite != frame) _image.sprite = frame;
        }
    }
}
