using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 왕궁 와사비 도박의 판정 미니게임. 플레이어가 가위/바위/보 버튼을 누르면
    /// 왕이 랜덤으로 한 손을 내고 즉시 승패를 가른다. 비기면 자동으로 재대결한다.
    /// RoyalWasabiController가 이 결과(승리 여부)를 받아 버프 적용/왕궁 연출로 이어간다.
    /// </summary>
    public class RockPaperScissorsPanel : MonoBehaviour
    {
        [Tooltip("패널 루트. 비워두면 이 오브젝트 자신을 켜고 끈다.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button rockButton;
        [SerializeField] private Button paperButton;
        [SerializeField] private Button scissorsButton;
        [Tooltip("대결 상태 안내(\"가위바위보!\", 비김 안내 등).")]
        [SerializeField] private Text promptText;
        [Tooltip("왕이 낸 손을 보여주는 텍스트. 비워두면 표시하지 않는다.")]
        [SerializeField] private Text opponentHandText;

        [SerializeField] private string promptMessage = "가위바위보!";
        [SerializeField] private string drawMessage = "비겼다! 다시 승부!";
        [Tooltip("왕의 손을 밝히기 전 실시간 대기(초).")]
        [SerializeField] private float revealDelay = 0.4f;
        [Tooltip("비겼을 때 재대결 전 실시간 대기(초).")]
        [SerializeField] private float drawRetryDelay = 0.8f;

        private readonly System.Random _random = new System.Random();
        private Action<bool> _onResolved;
        private Coroutine _routine;

        private GameObject Root => root != null ? root : gameObject;

        private void Awake()
        {
            if (rockButton != null) rockButton.onClick.AddListener(() => HandlePlayerPick(RpsHand.Rock));
            if (paperButton != null) paperButton.onClick.AddListener(() => HandlePlayerPick(RpsHand.Paper));
            if (scissorsButton != null) scissorsButton.onClick.AddListener(() => HandlePlayerPick(RpsHand.Scissors));

            // RoyalWasabiPanel/LevelUpPanel과 달리 이 패널은 root가 스크립트 자신의
            // GameObject라, 씬에서 비활성 상태로 시작한다. 여기서 Hide()를 부르면
            // Show()가 첫 SetActive(true)를 호출한 바로 그 프레임에 Awake가 실행되며
            // 도로 꺼버려서, 패널이 영영 켜지지 않는 버그가 생긴다.
        }

        private void OnDestroy()
        {
            if (_routine != null) StopCoroutine(_routine);
        }

        /// <summary>승부가 확정되면(비김 제외) onResolved(true=승리)를 한 번 호출한다.</summary>
        public void Show(Action<bool> onResolved)
        {
            _onResolved = onResolved;

            Root.SetActive(true);
            SetButtonsInteractable(true);

            if (promptText != null) promptText.text = promptMessage;
            if (opponentHandText != null) opponentHandText.text = string.Empty;
        }

        public void Hide() => Root.SetActive(false);

        private void HandlePlayerPick(RpsHand playerHand)
        {
            SetButtonsInteractable(false);

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ResolveRound(playerHand));
        }

        private IEnumerator ResolveRound(RpsHand playerHand)
        {
            RpsHand opponentHand = (RpsHand)_random.Next(3);

            yield return new WaitForSecondsRealtime(revealDelay);

            if (opponentHandText != null)
                opponentHandText.text = ToDisplayName(opponentHand);

            RpsOutcome outcome = RpsRules.Resolve(playerHand, opponentHand);

            if (outcome == RpsOutcome.Draw)
            {
                if (promptText != null) promptText.text = drawMessage;

                yield return new WaitForSecondsRealtime(drawRetryDelay);

                if (this == null) yield break;

                if (promptText != null) promptText.text = promptMessage;
                if (opponentHandText != null) opponentHandText.text = string.Empty;
                SetButtonsInteractable(true);
                _routine = null;
                yield break;
            }

            _routine = null;
            _onResolved?.Invoke(outcome == RpsOutcome.Win);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (rockButton != null) rockButton.interactable = interactable;
            if (paperButton != null) paperButton.interactable = interactable;
            if (scissorsButton != null) scissorsButton.interactable = interactable;
        }

        private static string ToDisplayName(RpsHand hand) => hand switch
        {
            RpsHand.Rock => "바위",
            RpsHand.Paper => "보",
            RpsHand.Scissors => "가위",
            _ => string.Empty
        };
    }
}
