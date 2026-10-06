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
        [Tooltip("와사비를 받았을 때 증강 목록 맨 끝에 같이 보여줄 아이콘. 비워두면 와사비는 표시하지 않는다.")]
        [SerializeField] private Sprite wasabiIcon;
        [Tooltip("증강 아이콘 뒤에 깔리는 두루마리. 9-slice(Sliced) 이미지여야 아이콘 개수만큼 좌우로 늘어난다. 비워두면 크기를 조정하지 않는다.")]
        [SerializeField] private RectTransform augmentListBackground;
        [Tooltip("아이콘 묶음 테두리 바깥으로 배경이 더 나오는 여백(px). x는 좌우 각각, y는 위아래 각각. 두루마리 말린 끝이 아이콘을 가리지 않게 그림의 테두리보다 크게 둔다.")]
        [SerializeField] private Vector2 augmentListBackgroundPadding = new Vector2(60f, 40f);
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
                           IReadOnlyList<AugmentCount> augments, int wasabiCount = 0)
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

            BuildAugmentList(augments, wasabiCount);
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

        private void BuildAugmentList(IReadOnlyList<AugmentCount> augments, int wasabiCount)
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

            if (wasabiCount > 0 && wasabiIcon != null)
            {
                ResultAugmentEntry wasabiEntry = Instantiate(augmentEntryPrefab, augmentListRoot);
                wasabiEntry.Bind(wasabiIcon, wasabiCount);
                _spawnedEntries.Add(wasabiEntry);
            }

            FitAugmentRow();
        }

        /// <summary>
        /// 아이콘 줄의 폭을 실제 개수만큼으로 맞추고 배경을 그 주위로 늘린다. 줄의 틀(augmentListRoot)을
        /// 줄 폭과 같게 줄여야 중앙 기준으로 좌우 대칭이 된다 — 틀이 고정 크기면 HorizontalLayoutGroup이
        /// 아이콘을 틀의 왼쪽 끝부터 채워서 개수가 늘수록 줄이 오른쪽으로 치우친다.
        /// </summary>
        private void FitAugmentRow()
        {
            int count = _spawnedEntries.Count;

            if (augmentListBackground != null)
                augmentListBackground.gameObject.SetActive(count > 0);

            if (count == 0) return;

            RectTransform entryRect = augmentEntryPrefab.GetComponent<RectTransform>();
            var layout = augmentListRoot.GetComponent<HorizontalLayoutGroup>();
            float spacing = layout != null ? layout.spacing : 0f;
            RectOffset padding = layout != null ? layout.padding : new RectOffset();

            float rowWidth = padding.left + padding.right + count * entryRect.sizeDelta.x + (count - 1) * spacing;
            float rowHeight = padding.top + padding.bottom + entryRect.sizeDelta.y;

            ((RectTransform)augmentListRoot).sizeDelta = new Vector2(rowWidth, rowHeight);

            if (augmentListBackground != null)
            {
                augmentListBackground.sizeDelta = new Vector2(
                    rowWidth + augmentListBackgroundPadding.x * 2f,
                    rowHeight + augmentListBackgroundPadding.y * 2f);
            }
        }

        public void HandleRestart()
        {
            // 결과 화면 다음은 곧장 캐릭터 선택이 아니라 부스 대기 화면(인트로)이다.
            Time.timeScale = 1f;
            SceneManager.LoadScene("IntroScene");
        }
    }
}