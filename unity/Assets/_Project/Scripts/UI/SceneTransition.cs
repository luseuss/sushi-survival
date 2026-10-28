using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 장면을 바꿀 때의 줌인 + 페이드 연출. Go(장면 이름)을 부르면 지금 화면을 캡처해 맨 위에 덮고, 그 화면이 커지며 어두워지는 동안
    /// 새 장면을 불러온 뒤 어둠이 걷히며 새 장면이 나타난다. 어느 장면에서든 쓸 수 있고 씬 배선이 필요 없다.
    /// 캡처에 실패하면 줌 없이 어두워지기만 한다. 전환 중에는 입력을 막는다. 시간은 실시간(unscaled)이다.
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        private static SceneTransition _instance;

        private Canvas _canvas;
        private RawImage _snapshot;
        private Image _blackout;
        private Texture2D _capture;
        private bool _busy;

        /// <summary>전환 중인지. 중복으로 시작하지 않게 막는 데 쓴다.</summary>
        public static bool IsBusy => _instance != null && _instance._busy;

        /// <summary>줌인 + 페이드로 sceneName 장면으로 넘어간다. 이미 전환 중이면 무시한다.</summary>
        public static void Go(string sceneName)
        {
            if (IsBusy) return;

            if (_instance == null)
            {
                var go = new GameObject("SceneTransition");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<SceneTransition>();
                _instance.BuildOverlay();
            }

            _instance.StartCoroutine(_instance.Run(sceneName));
        }

        private void BuildOverlay()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32000; // 어떤 UI보다 위
            gameObject.AddComponent<GraphicRaycaster>();

            _snapshot = CreateFullScreen<RawImage>("Snapshot");
            _snapshot.color = Color.white;
            _blackout = CreateFullScreen<Image>("Blackout");
            _blackout.color = new Color(0f, 0f, 0f, 0f);

            _canvas.enabled = false;
        }

        private T CreateFullScreen<T>(string objectName) where T : Graphic
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(T));
            go.transform.SetParent(transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var graphic = go.GetComponent<T>();
            graphic.raycastTarget = true; // 전환 중 뒤의 버튼이 눌리지 않게 막는다.
            return graphic;
        }

        private IEnumerator Run(string sceneName)
        {
            _busy = true;

            // 옛 화면을 그대로 캡처해 덮어 둔다(덮는 순간 화면은 그대로라 튀지 않는다).
            yield return new WaitForEndOfFrame();
            _capture = ScreenCapture.CaptureScreenshotAsTexture();
            _snapshot.texture = _capture;
            _snapshot.enabled = _capture != null;
            _snapshot.rectTransform.localScale = Vector3.one;
            _blackout.color = new Color(0f, 0f, 0f, 0f);
            _canvas.enabled = true;

            // 나가기: 화면이 커지며 어두워진다.
            for (float elapsed = 0f; elapsed < SceneTransitionLogic.OutDuration; elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f))
            {
                float t = SceneTransitionLogic.Progress(elapsed, SceneTransitionLogic.OutDuration);
                float zoom = SceneTransitionLogic.ZoomScale(t);
                _snapshot.rectTransform.localScale = new Vector3(zoom, zoom, 1f);
                _blackout.color = new Color(0f, 0f, 0f, SceneTransitionLogic.OutAlpha(t));
                yield return null;
            }

            _blackout.color = Color.black;
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            while (load != null && !load.isDone) yield return null;

            // 새 장면이 한 프레임 그려진 뒤, 옛 화면 캡처는 버리고 어둠만 걷는다.
            yield return null;
            _snapshot.enabled = false;
            if (_capture != null)
            {
                Destroy(_capture);
                _capture = null;
            }

            for (float elapsed = 0f; elapsed < SceneTransitionLogic.InDuration; elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f))
            {
                float t = SceneTransitionLogic.Progress(elapsed, SceneTransitionLogic.InDuration);
                _blackout.color = new Color(0f, 0f, 0f, SceneTransitionLogic.InAlpha(t));
                yield return null;
            }

            _blackout.color = new Color(0f, 0f, 0f, 0f);
            _canvas.enabled = false;
            _busy = false;
        }
    }
}
