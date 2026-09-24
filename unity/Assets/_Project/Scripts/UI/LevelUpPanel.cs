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
        [Tooltip("팝업 페이드인에 걸리는 실시간(초). Show() 직후 timeScale이 0이 되므로 " +
                 "반드시 실시간으로 진행한다.")]
        [SerializeField] private float showDuration = 0.15f;
        [Tooltip("카드 한 장이 커지며 등장하는 데 걸리는 실시간(초).")]
        [SerializeField] private float cardDuration = 0.28f;
        [Tooltip("카드 사이 등장 시차(초). 카드 → 왕궁 버튼 순으로 이만큼씩 늦게 나타난다.")]
        [SerializeField] private float cardStagger = 0.07f;

        private Coroutine _showRoutine;
        private Action _onRoyalWasabi;
        private CanvasGroup _canvasGroup;

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
            activeRoot.transform.localScale = Vector3.one;

            // 페이드용 CanvasGroup은 씬에 따로 배선하지 않고 필요할 때 붙인다.
            _canvasGroup = activeRoot.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = activeRoot.AddComponent<CanvasGroup>();

            // 등장 연출 중에 눌러서 의도치 않게 고르는 일이 없도록 잠근다.
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null) continue;

                if (i < options.Count)
                {
                    optionButtons[i].AppearScale = 0f;
                    optionButtons[i].Bind(options[i], onChosen);
                }
                else
                {
                    optionButtons[i].Clear();
                }
            }

            _onRoyalWasabi = onRoyalWasabi;
            if (royalWasabiButton != null)
            {
                SetWasabiScale(0f);
                royalWasabiButton.onClick.RemoveAllListeners();
                royalWasabiButton.onClick.AddListener(HandleRoyalWasabiClicked);
            }

            if (_showRoutine != null) StopCoroutine(_showRoutine);
            _showRoutine = StartCoroutine(PopIn());
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

        // 팝업이 페이드인하는 동안 카드 → 왕궁 버튼 순으로 시차를 두고 튀어나온다.
        private IEnumerator PopIn()
        {
            var cards = new List<LevelUpOptionButton>();
            foreach (var button in optionButtons)
            {
                if (button != null && button.gameObject.activeSelf)
                    cards.Add(button);
            }

            float wasabiDelay = cards.Count * cardStagger;
            float total = Mathf.Max(showDuration, wasabiDelay + cardDuration);
            float elapsed = 0f;

            while (elapsed < total)
            {
                if (this == null || _canvasGroup == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(elapsed / showDuration);

                for (int i = 0; i < cards.Count; i++)
                    cards[i].AppearScale = EaseOutBack(Mathf.Clamp01((elapsed - i * cardStagger) / cardDuration));

                SetWasabiScale(EaseOutBack(Mathf.Clamp01((elapsed - wasabiDelay) / cardDuration)));

                yield return null;
            }

            foreach (var card in cards)
                card.AppearScale = 1f;
            SetWasabiScale(1f);

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _showRoutine = null;
        }

        // 왕궁 버튼에는 ButtonJuice가 붙어 있어 호버 배율과 곱해서 써야 서로 덮어쓰지 않는다.
        private void SetWasabiScale(float scale)
        {
            if (royalWasabiButton == null) return;

            if (royalWasabiButton.TryGetComponent<ButtonJuice>(out var juice))
                juice.AppearScale = scale;
            else
                royalWasabiButton.transform.localScale = Vector3.one * scale;
        }

        // 1을 살짝 넘겼다가 안착한다(오버슈트). 팝업이 통통 튀며 나오는 느낌.
        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            float u = t - 1f;
            return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
        }
    }
}