using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SushiSurvival.UI
{
    /// <summary>
    /// UI 안에 영상을 그리는 표면. RawImage와 VideoPlayer를 함께 만들어 관리한다.
    /// 영상이 준비되고 첫 프레임이 나오기 전까지는 화면에 나오지 않아서, 로딩 중에 검은 화면이
    /// 번쩍이지 않고 원래 화면(카드 그림·배경)이 그대로 보인다. 재생에 실패해도 마찬가지다.
    /// 소리는 재생하지 않는다.
    /// </summary>
    public class VideoSurface : MonoBehaviour
    {
        private RawImage _image;
        private VideoPlayer _player;
        private bool _prepared;
        private bool _failed;
        private bool _playWhenPrepared;
        private bool _showOnNextFrame;

        /// <summary>parent 아래에 parent를 꽉 채우는 영상 표면을 만든다.</summary>
        public static VideoSurface Create(Transform parent, VideoClip clip, bool loop, string objectName)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var surface = go.AddComponent<VideoSurface>();
            surface.Init(clip, loop);
            return surface;
        }

        private void Init(VideoClip clip, bool loop)
        {
            _image = gameObject.AddComponent<RawImage>();
            _image.raycastTarget = false;
            _image.enabled = false;

            _player = gameObject.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.source = VideoSource.VideoClip;
            _player.clip = clip;
            _player.renderMode = VideoRenderMode.APIOnly;
            _player.audioOutputMode = VideoAudioOutputMode.None;
            _player.isLooping = loop;
            _player.skipOnDrop = true;
            _player.waitForFirstFrame = true;
            _player.sendFrameReadyEvents = true;

            _player.prepareCompleted += OnPrepared;
            _player.frameReady += OnFrameReady;
            _player.errorReceived += OnError;

            _player.Prepare();
        }

        private void OnDestroy()
        {
            if (_player == null) return;

            _player.prepareCompleted -= OnPrepared;
            _player.frameReady -= OnFrameReady;
            _player.errorReceived -= OnError;
        }

        /// <summary>처음부터 재생한다. 아직 준비 중이면 준비되는 즉시 시작한다.</summary>
        public void Play()
        {
            if (_failed) return;

            if (!_prepared)
            {
                _playWhenPrepared = true;
                return;
            }

            StartFromBeginning();
        }

        /// <summary>재생을 멈추고 화면에서 숨긴다(아래 원래 화면이 다시 보인다).</summary>
        public void StopAndHide()
        {
            _playWhenPrepared = false;
            _showOnNextFrame = false;

            if (_image != null) _image.enabled = false;

            if (_prepared && !_failed && _player.isPlaying)
                _player.Pause();
        }

        private void StartFromBeginning()
        {
            // 예전 프레임이 잠깐 비치지 않도록, 새 첫 프레임이 도착한 뒤에 보여준다.
            _showOnNextFrame = true;
            _player.frame = 0;
            _player.Play();
        }

        private void OnPrepared(VideoPlayer source)
        {
            _prepared = true;

            if (_playWhenPrepared)
            {
                _playWhenPrepared = false;
                StartFromBeginning();
            }
        }

        private void OnFrameReady(VideoPlayer source, long frameIdx)
        {
            if (!_showOnNextFrame) return;

            _showOnNextFrame = false;
            _image.texture = source.texture;
            _image.enabled = true;
        }

        private void OnError(VideoPlayer source, string message)
        {
            _failed = true;
            _image.enabled = false;
            Debug.LogWarning($"{name}: 영상을 재생할 수 없어 원래 화면을 유지합니다. {message}");
        }
    }
}
