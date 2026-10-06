using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 왕궁 배경 위에 대사를 잠깐 보여준 뒤 성공/실패 결과를 표시한다. 딤 오버레이(어두움+붉은 조명),
    /// 공개 순간 화면 번쩍임, 승리 후광, 패배 틴트를 이 패널이 전담한다 — RockPaperScissorsPanel과
    /// RoyalWasabiController는 이 연출을 모른다.
    /// </summary>
    public class RoyalWasabiPanel : MonoBehaviour
    {
        [Tooltip("패널 루트. 비워두면 이 오브젝트 자신을 켜고 끈다.")]
        [SerializeField] private GameObject root;
        [Tooltip("대사/결과 텍스트 뒤 배경 바. 왕궁 배경(Background)과 형제 오브젝트라 " +
                 "따로 꺼야 왕궁 배경이 가위바위보 중에도 유지된다.")]
        [SerializeField] private GameObject textBar;
        [Tooltip("대화창 안 작은 초상화 창. 비워두면 표시하지 않는다.")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text flavorText;
        [SerializeField] private Text resultText;
        [Tooltip("승리 시 적용된 강화 내용을 한 줄씩 순서대로 보여줄 Text. 비워두면 건너뛴다.")]
        [SerializeField] private Text buffListText;
        [Tooltip("화면 전체를 덮는 반투명 오버레이. 알현 내내 어둡게+붉게 깔고, 공개 순간엔 잠깐 밝아진다. 비워두면 그 연출만 건너뛴다.")]
        [SerializeField] private Image dimOverlay;
        [Tooltip("초상화 뒤에 까는 후광 링(승리 때만 반짝인다). 비워두면 건너뛴다.")]
        [SerializeField] private Image haloImage;
        [Tooltip("결과 확인 버튼. 처음엔 숨겨져 있다가 결과와 함께 나타난다.")]
        [SerializeField] private GameObject confirmButtonRoot;
        [SerializeField] private Button confirmButton;

        [Header("타이밍")]
        [Tooltip("대사만 보여주는 실시간 대기(초). Show() 시점에 이미 timeScale이 " +
                 "0이라 반드시 실시간으로 진행한다.")]
        [SerializeField] private float flavorDuration = 1.2f;
        [SerializeField] private string flavorMessage = "와사비를 하사받으러 왕을 알현합니다...";
        [SerializeField] private string successMessage = "빛나는 와사비를 하사받았다!";
        [SerializeField] private string failureMessage = "오늘은 빈손으로 돌아왔다...";

        [Header("딤 오버레이")]
        [Tooltip("평소 딤 색(어둡고 붉은 조명).")]
        [SerializeField] private Color dimColor = new Color(0.3f, 0f, 0f, 0.45f);
        [Tooltip("공개 순간 잠깐 밝아지는 색.")]
        [SerializeField] private Color flashColor = new Color(1f, 0.9f, 0.7f, 0.6f);
        [SerializeField] private float flashSeconds = 0.15f;
        [Tooltip("패배 시 잠깐 무채색에 가깝게(채도 빠짐을 흉내).")]
        [SerializeField] private Color desaturateColor = new Color(0.3f, 0.3f, 0.3f, 0.55f);
        [SerializeField] private float desaturateSeconds = 0.35f;

        [Header("진동")]
        [Tooltip("알현 진입 순간 짧은 진동.")]
        [SerializeField] private float entranceShakeMagnitude = 0.12f;
        [SerializeField] private float entranceShakeSeconds = 0.15f;
        [Tooltip("패배 시 화면 흔들림.")]
        [SerializeField] private float defeatShakeMagnitude = 0.2f;
        [SerializeField] private float defeatShakeSeconds = 0.2f;

        [Header("승리 연출")]
        [Tooltip("버프 한 줄이 나타나는 간격(초, 실시간).")]
        [SerializeField] private float buffLineInterval = 0.35f;
        [SerializeField] private float haloPulseSeconds = 0.6f;
        [SerializeField] private float haloMaxScale = 1.6f;

        private GameObject Root => root != null ? root : gameObject;
        private Action _onConfirm;
        private Coroutine _routine;
        private Coroutine _overlayRoutine;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirmClicked);

            if (haloImage != null)
                haloImage.gameObject.SetActive(false);

            Hide();
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        /// <summary>
        /// 가위바위보 시작 전 "알현" 대사만 보여준다. flavorDuration이 지나면
        /// onFlavorDone을 불러 호출자가 RPS 패널로 넘어가게 한다.
        /// </summary>
        public void ShowFlavor(Sprite portrait, Action onFlavorDone)
        {
            Root.SetActive(true);
            ShowDialogueBox();
            SetDim(dimColor);

            if (JuiceDirector.Instance != null)
                JuiceDirector.Instance.Shake(entranceShakeMagnitude, entranceShakeSeconds);

            if (portraitImage != null)
                portraitImage.sprite = portrait;

            if (flavorText != null)
                flavorText.text = flavorMessage;

            if (resultText != null)
                resultText.text = string.Empty;

            if (buffListText != null)
                buffListText.text = string.Empty;

            if (confirmButtonRoot != null)
                confirmButtonRoot.SetActive(false);

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(WaitThenInvoke(flavorDuration, onFlavorDone));
        }

        /// <summary>
        /// 가위바위보가 진행되는 동안 대화상자(텍스트바/대사/결과/확인버튼)만 숨긴다.
        /// Root를 통째로 끄면 왕궁 배경(Background)도 같이 꺼져버려서 따로 둔다.
        /// </summary>
        public void HideDialogueBox()
        {
            if (textBar != null) textBar.SetActive(false);
            if (flavorText != null) flavorText.gameObject.SetActive(false);
            if (resultText != null) resultText.gameObject.SetActive(false);
            if (confirmButtonRoot != null) confirmButtonRoot.SetActive(false);
        }

        private void ShowDialogueBox()
        {
            if (textBar != null) textBar.SetActive(true);
            if (flavorText != null) flavorText.gameObject.SetActive(true);
            if (resultText != null) resultText.gameObject.SetActive(true);
        }

        /// <summary>
        /// 가위바위보 손을 공개하는 순간마다(비겨서 재대결해도 매번) 부른다. 화면이 잠깐 밝아졌다가
        /// 딤 색으로 돌아온다. 히트스톱은 RoyalWasabiController가 JuiceDirector로 직접 건다.
        /// </summary>
        public void FlashReveal()
        {
            if (dimOverlay == null) return;

            if (_overlayRoutine != null) StopCoroutine(_overlayRoutine);
            _overlayRoutine = StartCoroutine(FadeOverlay(flashColor, dimColor, flashSeconds));
        }

        /// <summary>
        /// 가위바위보 결과가 나온 뒤 성공/실패 문구와 확인 버튼을 보여준다. appliedLines가 있으면
        /// (승리 시) 한 줄씩 순서대로 나타나고 후광이 반짝인다. 실패 시엔 흔들림+짧은 채도 빠짐만 준다.
        /// </summary>
        public void ShowResult(bool success, Sprite portrait, string[] appliedLines, Action onConfirm)
        {
            _onConfirm = onConfirm;

            Root.SetActive(true);
            ShowDialogueBox();

            if (portraitImage != null)
                portraitImage.sprite = portrait;

            // flavorText를 지우지 않으면 resultText와 같은 자리에 겹쳐 보인다
            // (두 Text의 RectTransform이 같은 위치에 겹쳐 배치돼 있음).
            if (flavorText != null)
                flavorText.text = string.Empty;

            if (resultText != null)
                resultText.text = success ? successMessage : failureMessage;

            if (confirmButtonRoot != null)
                confirmButtonRoot.SetActive(false);

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(success ? RevealVictory(appliedLines) : RevealDefeat());
        }

        private IEnumerator RevealVictory(string[] appliedLines)
        {
            if (haloImage != null)
                StartCoroutine(PulseHalo());

            if (buffListText != null && appliedLines != null)
            {
                buffListText.text = string.Empty;

                foreach (string line in appliedLines)
                {
                    if (string.IsNullOrEmpty(line)) continue;

                    buffListText.text += (buffListText.text.Length > 0 ? "\n" : string.Empty) + line;
                    FlashReveal();

                    yield return new WaitForSecondsRealtime(buffLineInterval);
                }
            }

            _routine = null;
            ShowConfirmButton();
        }

        private IEnumerator PulseHalo()
        {
            haloImage.gameObject.SetActive(true);
            haloImage.transform.localScale = Vector3.one;
            SetAlpha(haloImage, 1f);

            float elapsed = 0f;
            while (elapsed < haloPulseSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / haloPulseSeconds);

                haloImage.transform.localScale = Vector3.one * Mathf.Lerp(1f, haloMaxScale, progress);
                SetAlpha(haloImage, 1f - progress);

                yield return null;
            }

            haloImage.gameObject.SetActive(false);
        }

        private IEnumerator RevealDefeat()
        {
            if (JuiceDirector.Instance != null)
                JuiceDirector.Instance.Shake(defeatShakeMagnitude, defeatShakeSeconds);

            if (dimOverlay != null)
            {
                if (_overlayRoutine != null) StopCoroutine(_overlayRoutine);
                _overlayRoutine = StartCoroutine(FadeOverlay(desaturateColor, dimColor, desaturateSeconds));
            }

            yield return new WaitForSecondsRealtime(desaturateSeconds);

            _routine = null;
            ShowConfirmButton();
        }

        private void ShowConfirmButton()
        {
            if (confirmButtonRoot != null)
                confirmButtonRoot.SetActive(true);
        }

        public void Hide()
        {
            Root.SetActive(false);
            SetDim(Color.clear);

            if (haloImage != null)
                haloImage.gameObject.SetActive(false);
        }

        private void SetDim(Color color)
        {
            if (dimOverlay == null) return;

            if (_overlayRoutine != null)
            {
                StopCoroutine(_overlayRoutine);
                _overlayRoutine = null;
            }

            dimOverlay.color = color;
        }

        private IEnumerator FadeOverlay(Color from, Color to, float seconds)
        {
            dimOverlay.color = from;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                dimOverlay.color = Color.Lerp(from, to, Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }

            dimOverlay.color = to;
            _overlayRoutine = null;
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private IEnumerator WaitThenInvoke(float delay, Action callback)
        {
            yield return new WaitForSecondsRealtime(delay);
            _routine = null;
            callback?.Invoke();
        }

        private void HandleConfirmClicked() => _onConfirm?.Invoke();
    }
}
