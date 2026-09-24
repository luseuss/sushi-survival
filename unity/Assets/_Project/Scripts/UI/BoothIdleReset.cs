using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 부스용 무입력 자동 리셋. 관람객이 자리를 떠나면 화면이 스토리·전투·결과 어디에 멈춰 있든 인트로로 돌려서
    /// 다음 관람객이 바로 시작할 수 있게 한다. 첫 씬이 뜰 때 스스로 만들어져 씬이 바뀌어도 유지되므로
    /// 씬 배선이 필요 없다. 시간·판정은 IdleResetController가 맡고, 여기서는 입력을 읽어 넘기고 리셋을 실행한다.
    /// </summary>
    public class BoothIdleReset : MonoBehaviour
    {
        private const string SettingsResourceName = "BoothResetSettings";
        private const float MouseMoveThreshold = 1f;

        private BoothResetSettings _settings;
        private IdleResetController _controller;

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

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        // 씬이 바뀐 것 자체가 활동이다. 로딩 시간이 무입력으로 쌓이지 않게 한다.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => _controller.NotifyActivity();

        // 정지(timeScale 0) 중에도 시간이 가야 하므로 unscaled를 쓴다. 로딩 직후 큰 dt는 상한을 둔다.
        private void Update() => _controller.Tick(Mathf.Min(Time.unscaledDeltaTime, 0.25f), InputDetected());

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
