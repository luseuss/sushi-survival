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

        private Coroutine _showRoutine;
        private Action _onRoyalWasabi;

        public static LevelUpPanel Instance { get; private set; }

        private GameObject GetRoot()
        {
            if (this == null) return null;
            if (root != null) return root;
            try
            {
                if (gameObject != null) return gameObject;
            }
            catch
            {
                // 이미 파괴된 객체 접근 시 예외 방어
            }
            return null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }
        }

        public void Show(IReadOnlyList<IUpgradeOption> options, Action<IUpgradeOption> onChosen,
                           Action onRoyalWasabi)
        {
            GameObject activeRoot = GetRoot();
            if (activeRoot == null) return;

            activeRoot.SetActive(true);
            activeRoot.transform.localScale = Vector3.zero;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null) continue;

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

        public void Hide()
        {
            GameObject activeRoot = GetRoot();
            if (activeRoot != null)
            {
                activeRoot.SetActive(false);
            }
        }

        private void HandleRoyalWasabiClicked() => _onRoyalWasabi?.Invoke();

        private IEnumerator ScaleIn()
        {
            GameObject activeRoot = GetRoot();
            if (activeRoot == null) yield break;

            Transform t = activeRoot.transform;
            float elapsed = 0f;

            while (elapsed < showDuration)
            {
                if (this == null || t == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / showDuration));
                t.localScale = Vector3.one * p;
                yield return null;
            }

            if (t != null)
            {
                t.localScale = Vector3.one;
            }
            _showRoutine = null;
        }
    }
}