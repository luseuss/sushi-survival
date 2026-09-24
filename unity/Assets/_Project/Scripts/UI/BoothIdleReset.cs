using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 부스용 무입력 자동 리셋. 관람객이 자리를 떠나면 화면이 스토리·전투·결과 어디에 멈춰 있든 인트로로 돌려서
    /// 다음 관람객이 바로 시작할 수 있게 한다. 리셋 직전에는 남은 시간을 화면 아래에 안내해서, 읽다가
    /// 갑자기 튕기지 않게 하고 입력하면 안내가 사라진다. 첫 씬이 뜰 때 스스로 만들어져 씬이 바뀌어도
    /// 유지되므로 씬 배선이 필요 없다. 시간·판정은 IdleResetController가 맡고, 여기서는 입력을 읽어 넘기고
    /// 리셋을 실행하며 안내를 그린다.
    /// </summary>
    public class BoothIdleReset : MonoBehaviour
    {
        private const string SettingsResourceName = "BoothResetSettings";
        private const float MouseMoveThreshold = 1f;
        private const int OverlaySortingOrder = 5000;

        private BoothResetSettings _settings;
        private IdleResetController _controller;
        private GameObject _warningRoot;
        private Text _warningText;
        private int _shownNumber = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindObjectOfType<BoothIdleReset>() != null) return;

            var settings = LoadSettings();
            if (!settings.enabled) return;
            if (Application.isEditor && !settings.activeInEditor) return;

            var go = new GameObject("BoothIdleReset");
            DontDestroyOnLoad(go);
            go.AddComponent<BoothIdleReset>();
        }

        private static BoothResetSettings LoadSettings()
        {
            var loaded = Resources.Load<BoothResetSettings>(SettingsResourceName);
            return loaded != null ? loaded : ScriptableObject.CreateInstance<BoothResetSettings>();
        }

        private void Awake()
        {
            _settings = LoadSettings();
            _controller = new IdleResetController(CurrentContext, () => _settings.ToTimeouts(), ResetToIntro);
            BuildWarningOverlay();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        // 씬이 바뀐 것 자체가 활동이다. 로딩 시간이 무입력으로 쌓이지 않게 한다.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _controller.NotifyActivity();
            RefreshWarning();
        }

        // 정지(timeScale 0) 중에도 시간이 가야 하므로 unscaled를 쓴다. 로딩 직후 큰 dt는 상한을 둔다.
        private void Update()
        {
            _controller.Tick(Mathf.Min(Time.unscaledDeltaTime, 0.25f), InputDetected());
            RefreshWarning();
        }

        // 안내는 남은 시간이 안내 구간에 들어왔을 때만 보이고, 숫자가 바뀔 때만 글자를 갱신한다.
        private void RefreshWarning()
        {
            bool show = IdleResetLogic.ShouldWarn(_controller.RemainingSeconds, _settings.warningSeconds);

            if (_warningRoot.activeSelf != show)
                _warningRoot.SetActive(show);

            if (!show)
            {
                _shownNumber = -1;
                return;
            }

            int number = IdleResetLogic.CountdownNumber(_controller.RemainingSeconds);
            if (number == _shownNumber) return;

            _shownNumber = number;
            _warningText.text = $"입력이 없어 {number}초 뒤 처음 화면으로 돌아갑니다\n화면을 누르거나 움직이면 계속할 수 있어요";
        }

        // 안내 UI는 씬에 두지 않고 여기서 만든다 — 어느 씬에서나 같은 모습으로 맨 위에 나와야 해서다.
        private void BuildWarningOverlay()
        {
            var canvasGo = new GameObject("IdleWarning", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var bandGo = new GameObject("Band", typeof(RectTransform), typeof(Image));
            bandGo.transform.SetParent(canvasGo.transform, false);

            var band = (RectTransform)bandGo.transform;
            band.anchorMin = new Vector2(0.5f, 0f);
            band.anchorMax = new Vector2(0.5f, 0f);
            band.pivot = new Vector2(0.5f, 0f);
            band.sizeDelta = new Vector2(1200f, 130f);
            band.anchoredPosition = new Vector2(0f, 60f);

            var bandImage = bandGo.GetComponent<Image>();
            bandImage.color = new Color(0f, 0f, 0f, 0.82f);
            bandImage.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(bandGo.transform, false);

            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(30f, 10f);
            textRect.offsetMax = new Vector2(-30f, -10f);

            _warningText = textGo.GetComponent<Text>();
            _warningText.font = _settings.warningFont != null
                ? _settings.warningFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _warningText.fontSize = 36;
            _warningText.alignment = TextAnchor.MiddleCenter;
            _warningText.color = new Color(0.949f, 0.851f, 0.4f, 1f);
            _warningText.raycastTarget = false;
            _warningText.lineSpacing = 1.15f;

            _warningRoot = canvasGo;
            _warningRoot.SetActive(false);
        }

        private static IdleContext CurrentContext()
        {
            var manager = GameManager.Instance;
            return IdleResetLogic.ContextFor(
                SceneManager.GetActiveScene().name,
                manager != null,
                manager != null ? manager.CurrentState : RunState.CharacterSelect);
        }

        // 결과 화면의 "돌아가기" 버튼(ResultPanel.HandleRestart)과 같은 방식으로 처음 화면으로 돌아간다.
        private static void ResetToIntro()
        {
            Debug.Log("[BoothIdleReset] 입력이 없어 인트로로 돌아갑니다.");

            Time.timeScale = 1f;
            SceneManager.LoadScene(IdleResetLogic.IntroScene);
        }

        private static bool InputDetected()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.isPressed) return true;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed) return true;
                if (mouse.delta.ReadValue().sqrMagnitude > MouseMoveThreshold) return true;
                if (mouse.scroll.ReadValue().sqrMagnitude > 0f) return true;
            }

            // 게임패드는 버튼과 스틱 방향(눌림 지점을 넘긴 것)을 입력으로 본다. 스틱의 미세한 드리프트는 무시된다.
            foreach (var gamepad in Gamepad.all)
            {
                foreach (var control in gamepad.allControls)
                {
                    if (control is ButtonControl button && button.isPressed) return true;
                }
            }

            return false;
        }
    }
}
