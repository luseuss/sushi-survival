using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 왕궁 배경 위에 대사를 잠깐 보여준 뒤 성공/실패 결과를 표시한다.
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
        [Tooltip("결과 확인 버튼. 처음엔 숨겨져 있다가 결과와 함께 나타난다.")]
        [SerializeField] private GameObject confirmButtonRoot;
        [SerializeField] private Button confirmButton;

        [Tooltip("대사만 보여주는 실시간 대기(초). Show() 시점에 이미 timeScale이 " +
                 "0이라 반드시 실시간으로 진행한다.")]
        [SerializeField] private float flavorDuration = 1.2f;
        [SerializeField] private string flavorMessage = "와사비를 하사받으러 왕을 알현합니다...";
        [SerializeField] private string successMessage = "빛나는 와사비를 하사받았다!";
        [SerializeField] private string failureMessage = "오늘은 빈손으로 돌아왔다...";

        private GameObject Root => root != null ? root : gameObject;
        private Action _onConfirm;
        private Coroutine _routine;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirmClicked);

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

            if (portraitImage != null)
                portraitImage.sprite = portrait;

            if (flavorText != null)
                flavorText.text = flavorMessage;

            if (resultText != null)
                resultText.text = string.Empty;

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

        /// <summary>가위바위보 결과가 나온 뒤 성공/실패 문구와 확인 버튼을 보여준다.</summary>
        public void ShowResult(bool success, Sprite portrait, Action onConfirm)
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
                confirmButtonRoot.SetActive(true);
        }

        public void Hide() => Root.SetActive(false);

        private IEnumerator WaitThenInvoke(float delay, Action callback)
        {
            yield return new WaitForSecondsRealtime(delay);
            _routine = null;
            callback?.Invoke();
        }

        private void HandleConfirmClicked() => _onConfirm?.Invoke();
    }
}
