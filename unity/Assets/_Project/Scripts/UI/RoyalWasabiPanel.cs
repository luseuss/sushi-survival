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

        public void Show(bool success, Action onConfirm)
        {
            _onConfirm = onConfirm;

            Root.SetActive(true);

            if (flavorText != null)
                flavorText.text = flavorMessage;

            if (resultText != null)
                resultText.text = string.Empty;

            if (confirmButtonRoot != null)
                confirmButtonRoot.SetActive(false);

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(RevealResult(success));
        }

        public void Hide() => Root.SetActive(false);

        private IEnumerator RevealResult(bool success)
        {
            yield return new WaitForSecondsRealtime(flavorDuration);

            if (resultText != null)
                resultText.text = success ? successMessage : failureMessage;

            if (confirmButtonRoot != null)
                confirmButtonRoot.SetActive(true);

            _routine = null;
        }

        private void HandleConfirmClicked() => _onConfirm?.Invoke();
    }
}
