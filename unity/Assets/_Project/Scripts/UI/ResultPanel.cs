using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

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
        [Tooltip("증강 항목이 생성될 부모. Horizontal Layout Group을 붙여두면 자동 정렬된다.")]
        [SerializeField] private Transform augmentListRoot;
        [SerializeField] private ResultAugmentEntry augmentEntryPrefab;
        [SerializeField] private Button restartButton;

        private readonly List<ResultAugmentEntry> _spawnedEntries = new List<ResultAugmentEntry>();

        private GameObject Root => root != null ? root : gameObject;

        private void Awake()
        {
            Hide();

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
        }

        public void Hide() => Root.SetActive(false);

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
        }

        private void HandleRestart()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Restart();
        }
    }
}
