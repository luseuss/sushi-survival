using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 캐릭터 선택 화면 위에 띄우는 설명창. 열기 버튼은 캐릭터 선택 화면이 떠 있는 동안만 보이고, 누르면
    /// "게임플레이", "증강", "와사비·요정" 세 탭이 있는 설명창이 나온다. 닫기 버튼이나 ESC로 닫는다.
    /// - 게임플레이: 씬에 직접 쓴 내용(조작·한 줄 요약 등)을 담은 오브젝트를 켜고 끈다.
    /// - 증강: 증강 목록(처음 열 때 한 번만 만든다).
    /// - 와사비·요정: 와사비 도전 설명 몇 줄 + 펫 목록(카탈로그에서 만든다).
    /// 설명창 루트와 열기 버튼은 꺼질 수 있으므로, 이 컴포넌트는 항상 켜져 있는 오브젝트에 붙인다.
    /// </summary>
    public class AugmentGuidePanel : MonoBehaviour
    {
        private static readonly Color SelectedTabColor = new Color(0.55f, 0.35f, 0.15f, 1f);
        private static readonly Color IdleTabColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [Tooltip("이 패널이 떠 있는 동안에만 열기 버튼을 보여준다. 캐릭터 선택 패널을 넣는다.")]
        [SerializeField] private GameObject characterSelectPanel;
        [Tooltip("캐릭터 선택 화면이 나타나는 순간 설명창을 바로 띄운다. 닫은 뒤에는 열기 버튼으로 다시 연다.")]
        [SerializeField] private bool openWhenSelectShown = true;

        [Header("탭 버튼")]
        [SerializeField] private Button gameplayTabButton;
        [SerializeField] private Button augmentTabButton;
        [SerializeField] private Button fairyTabButton;

        [Header("게임플레이 탭")]
        [Tooltip("조작·한 줄 요약·일러스트 등 씬에 직접 쓴 내용을 묶은 오브젝트. 게임플레이 탭에서만 켜진다.")]
        [SerializeField] private GameObject gameplayRoot;

        [Header("목록 탭(증강·와사비·요정)")]
        [Tooltip("스크롤 목록 전체(ScrollRect 오브젝트). 게임플레이 탭에서는 꺼진다. 비워두면 켜고 끄지 않는다.")]
        [SerializeField] private GameObject listRoot;
        [SerializeField] private Transform content;

        [Header("증강 탭")]
        [Tooltip("설명창에 보여줄 증강. 위에서부터 이 순서대로 나온다.")]
        [SerializeField] private AugmentData[] augments;
        [SerializeField] private AugmentGuideEntry augmentEntryPrefab;

        [Header("와사비·요정 탭")]
        [Tooltip("목록 맨 위에 나오는 와사비 도전 설명. 한 줄에 한 항목.")]
        [TextArea(1, 3)]
        [SerializeField] private string[] fairyIntroLines =
        {
            "레벨업 카드 대신 4번째 버튼으로 왕에게 와사비를 빌러 갈 수 있습니다.",
            "가위바위보에서 이기면 초밥 펫이 따라다니며 함께 싸웁니다. 지면 그 레벨업의 보상은 없습니다.",
            "펫은 최대 3마리까지 데려갈 수 있고, 3마리가 모이면 이후에는 펫 강화 카드가 나옵니다."
        };
        [Tooltip("소개할 펫 종류. FairyController의 카탈로그와 같은 에셋을 같은 순서로 넣는다.")]
        [SerializeField] private FairyData[] fairyCatalog;
        [SerializeField] private FairyGuideEntry fairyEntryPrefab;
        [Tooltip("설명 줄에 쓰는 글자 프리팹. 비워두면 설명 줄을 만들지 않는다.")]
        [SerializeField] private Text headerPrefab;

        private readonly List<GameObject> _augmentRows = new List<GameObject>();
        private readonly List<GameObject> _fairyRows = new List<GameObject>();
        private bool _built;
        private bool _selectWasVisible;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (gameplayTabButton != null) gameplayTabButton.onClick.AddListener(ShowGameplay);
            if (augmentTabButton != null) augmentTabButton.onClick.AddListener(ShowAugments);
            if (fairyTabButton != null) fairyTabButton.onClick.AddListener(ShowFairy);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (gameplayTabButton != null) gameplayTabButton.onClick.RemoveListener(ShowGameplay);
            if (augmentTabButton != null) augmentTabButton.onClick.RemoveListener(ShowAugments);
            if (fairyTabButton != null) fairyTabButton.onClick.RemoveListener(ShowFairy);
        }

        private void Update()
        {
            // 캐릭터 선택 화면이 새로 나타난 프레임에 설명창을 자동으로 연다.
            bool selectVisible = characterSelectPanel != null && characterSelectPanel.activeInHierarchy;
            if (openWhenSelectShown && selectVisible && !_selectWasVisible)
                Open();
            _selectWasVisible = selectVisible;

            bool open = panelRoot != null && panelRoot.activeSelf;

            // 설명창을 연 동안과 캐릭터 선택이 끝난 뒤에는 열기 버튼을 숨긴다.
            if (openButton != null)
            {
                bool showButton = !open && (characterSelectPanel == null || characterSelectPanel.activeInHierarchy);
                if (openButton.gameObject.activeSelf != showButton)
                    openButton.gameObject.SetActive(showButton);
            }

            if (!open) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                Close();
        }

        /// <summary>설명창을 열고 게임플레이 탭부터 보여준다.</summary>
        public void Open()
        {
            if (panelRoot == null) return;

            BuildRows();
            ShowGameplay();
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void ShowGameplay() => ShowTab(GuideTab.Gameplay);

        public void ShowAugments() => ShowTab(GuideTab.Augments);

        public void ShowFairy() => ShowTab(GuideTab.Fairy);

        private void ShowTab(GuideTab tab)
        {
            if (gameplayRoot != null) gameplayRoot.SetActive(GuideTabLogic.ShowsGameplayRoot(tab));
            if (listRoot != null) listRoot.SetActive(GuideTabLogic.ShowsList(tab));

            bool augmentRows = GuideTabLogic.ShowsAugmentRows(tab);
            bool fairyRows = GuideTabLogic.ShowsFairyRows(tab);
            foreach (GameObject row in _augmentRows) row.SetActive(augmentRows);
            foreach (GameObject row in _fairyRows) row.SetActive(fairyRows);

            Tint(gameplayTabButton, tab == GuideTab.Gameplay);
            Tint(augmentTabButton, tab == GuideTab.Augments);
            Tint(fairyTabButton, tab == GuideTab.Fairy);

            // 탭을 바꾸면 목록 맨 위부터 보여준다.
            ScrollRect scroll = content != null ? content.GetComponentInParent<ScrollRect>() : null;
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        private static void Tint(Button button, bool selected)
        {
            if (button != null && button.TryGetComponent<Image>(out var image))
                image.color = selected ? SelectedTabColor : IdleTabColor;
        }

        private void BuildRows()
        {
            if (_built || content == null) return;
            _built = true;

            if (augmentEntryPrefab != null && augments != null)
            {
                foreach (AugmentData data in augments)
                {
                    if (data == null) continue;
                    AugmentGuideEntry entry = Instantiate(augmentEntryPrefab, content);
                    entry.Bind(data);
                    _augmentRows.Add(entry.gameObject);
                }
            }

            if (headerPrefab != null && fairyIntroLines != null)
            {
                foreach (string line in fairyIntroLines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    Text text = Instantiate(headerPrefab, content);
                    text.text = line;
                    _fairyRows.Add(text.gameObject);
                }
            }

            if (fairyEntryPrefab != null && fairyCatalog != null)
            {
                foreach (FairyData data in fairyCatalog)
                {
                    if (data == null) continue;
                    FairyGuideEntry entry = Instantiate(fairyEntryPrefab, content);
                    entry.Bind(data);
                    _fairyRows.Add(entry.gameObject);
                }
            }
        }
    }
}
