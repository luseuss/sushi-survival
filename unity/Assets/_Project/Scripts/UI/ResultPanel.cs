using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SushiSurvival.Core;
using SushiSurvival.Data;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    public class ResultPanel : MonoBehaviour
    {
        [Tooltip("결과 화면 루트. 비워두면 이 오브젝트 자신을 켜고 끈다.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text outcomeText;
        [SerializeField] private Text survivalTimeText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text killCountText;
        [Tooltip("세계관 대화에서 쌓인 성향에 맞는 한마디를 보여줄 Text. 비워두면 표시하지 않는다.")]
        [SerializeField] private Text traitText;
        [Tooltip("증강 항목이 생성될 부모. Horizontal Layout Group을 붙여두면 자동 정렬된다.")]
        [SerializeField] private Transform augmentListRoot;
        [SerializeField] private ResultAugmentEntry augmentEntryPrefab;
        [Tooltip("증강 아이콘 뒤에 깔리는 배경. 비워두면 크기 조정 없이 그대로 둔다.")]
        [SerializeField] private RectTransform augmentListBackground;
        [SerializeField] private Button restartButton;

        private readonly List<ResultAugmentEntry> _spawnedEntries = new List<ResultAugmentEntry>();

        private GameObject Root => root != null ? root : gameObject;

        private void Awake()
        {
            if (restartButton != null)
                restartButton.onClick.AddListener(HandleRestart);
        }

        private void OnDestroy()
        {
            if (restartButton != null)
                restartButton.onClick.RemoveListener(HandleRestart);
        }

        public void Show(RunOutcome outcome, float elapsed, int level, int kills,
                           IReadOnlyList<AugmentCount> augments)
        {
            Root.SetActive(true);

            if (outcomeText != null)
                outcomeText.text = outcome == RunOutcome.Victory ? "생존 성공!" : "패배";

            if (survivalTimeText != null)
                survivalTimeText.text = $"생존 시간  {RunClock.FormatElapsed(elapsed)}";

            if (levelText != null)
                levelText.text = $"도달 레벨  {level}";

            if (killCountText != null)
                killCountText.text = $"처치 수  {kills}";

            BuildAugmentList(augments);
            ShowTraitLine(outcome);
        }

        public void Hide() => Root.SetActive(false);

        private void ShowTraitLine(RunOutcome outcome)
        {
            if (traitText == null) return;

            PlayerTraitLines table = PlayerTraitLines.Load();
            PlayerTraitEntry entry = table != null ? table.Find(PlayerTraitState.Dominant) : null;

            string line = entry == null
                ? null
                : (outcome == RunOutcome.Victory ? entry.victoryLine : entry.defeatLine);

            bool hasLine = !string.IsNullOrEmpty(line);
            traitText.gameObject.SetActive(hasLine);

            if (hasLine)
                traitText.text = $"[{entry.displayName}] {line}";
        }

        private void BuildAugmentList(IReadOnlyList<AugmentCount> augments)
        {
            if (augmentListRoot == null || augmentEntryPrefab == null) return;

            foreach (var entry in _spawnedEntries)
            {
                if (entry != null)
                    Destroy(entry.gameObject);
            }
            _spawnedEntries.Clear();

            foreach (var augment in augments)
            {
                ResultAugmentEntry entry = Instantiate(augmentEntryPrefab, augmentListRoot);
                entry.Bind(augment);
                _spawnedEntries.Add(entry);
            }

            ResizeAugmentListBackground(augments.Count);
        }

        /// <summary>
        /// 배경을 AugmentListRoot에 실제로 나온 개수만큼만 늘린다 — HorizontalLayoutGroup이
        /// 아이콘을 간격 없이(spacing 0) 나열하는 것과 같은 폭 계산을 그대로 따라 한다.
        /// </summary>
        private void ResizeAugmentListBackground(int count)
        {
            if (augmentListBackground == null || augmentEntryPrefab == null) return;

            RectTransform entryRect = augmentEntryPrefab.GetComponent<RectTransform>();
            float spacing = 0f;
            RectOffset padding = null;

            var layout = augmentListRoot != null ? augmentListRoot.GetComponent<HorizontalLayoutGroup>() : null;
            if (layout != null)
            {
                spacing = layout.spacing;
                padding = layout.padding;
            }

            float paddingH = padding != null ? padding.left + padding.right : 0f;
            float paddingV = padding != null ? padding.top + padding.bottom : 0f;

            float width = count <= 0 ? 0f : count * entryRect.sizeDelta.x + Mathf.Max(0, count - 1) * spacing;
            width += paddingH;
            float height = entryRect.sizeDelta.y + paddingV;

            augmentListBackground.sizeDelta = new Vector2(width, height);
        }

        public void HandleRestart()
        {
            // 결과 화면 다음은 곧장 캐릭터 선택이 아니라 부스 대기 화면(인트로)이다.
            Time.timeScale = 1f;
            SceneManager.LoadScene("IntroScene");
        }
    }
}