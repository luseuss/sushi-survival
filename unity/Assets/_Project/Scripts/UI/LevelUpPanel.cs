using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    public class LevelUpPanel : MonoBehaviour
    {
        [Tooltip("팝업 루트. 비워두면 이 오브젝트 자신을 켜고 끈다.")]
        [SerializeField] private GameObject root;
        [Tooltip("선택지 버튼 3개.")]
        [SerializeField] private LevelUpOptionButton[] optionButtons;
        [Tooltip("카드 3장과 무관하게 항상 켜져 있는 4번째 선택지. 증강 풀이 " +
                 "고갈돼도 도박은 언제나 가능하다.")]
        [SerializeField] private UnityEngine.UI.Button royalWasabiButton;
        [Tooltip("스케일인에 걸리는 실시간(초). Show() 직후 timeScale이 0이 되므로 " +
                 "반드시 실시간으로 진행한다.")]
        [SerializeField] private float showDuration = 0.15f;

        private GameObject Root => root != null ? root : gameObject;
        private Coroutine _showRoutine;
        private Action _onRoyalWasabi;

        private void Awake() => Hide();

        public void Show(IReadOnlyList<IUpgradeOption> options, Action<IUpgradeOption> onChosen,
                         Action onRoyalWasabi)
        {
            Root.SetActive(true);
            Root.transform.localScale = Vector3.zero;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < options.Count)
                    optionButtons[i].Bind(options[i], onChosen);
                else
                    optionButtons[i].Clear();
            }

            _onRoyalWasabi = onRoyalWasabi;
            if (royalWasabiButton != null)
            {
                royalWasabiButton.onClick.RemoveAllListeners();
                royalWasabiButton.onClick.AddListener(HandleRoyalWasabiClicked);
            }

            if (_showRoutine != null) StopCoroutine(_showRoutine);
            _showRoutine = StartCoroutine(ScaleIn());
        }

        public void Hide() => Root.SetActive(false);

        private void HandleRoyalWasabiClicked() => _onRoyalWasabi?.Invoke();

        private IEnumerator ScaleIn()
        {
            Transform t = Root.transform;
            float elapsed = 0f;

            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / showDuration));
                t.localScale = Vector3.one * p;
                yield return null;
            }

            t.localScale = Vector3.one;
            _showRoutine = null;
        }
    }
}
