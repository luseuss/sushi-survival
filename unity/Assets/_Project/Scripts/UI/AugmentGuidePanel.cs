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
        // 탭 버튼 이미지는 그림 자체 색을 살려야 하므로, 선택은 원본 색(흰색), 비선택은 살짝 어둡게만 곱한다.
        private static readonly Color SelectedTabColor = Color.white;
        private static readonly Color IdleTabColor = new Color(0.6f, 0.6f, 0.6f, 1f);

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
            "가위바위보에서 이기면 무기가 그 자리에서 변신하고 공격력이 25% 더 붙습니다. 지면 그 레벨업의 보상은 없습니다.",
            "무기는 강화 3레벨에서도 저절로 변신하지만, 와사비로 일찍 변신한 무기가 더 강합니다. 이미 변신했다면 와사비 버튼은 나오지 않습니다.",
            "요정은 시작 대화에서 성향에 맞는 후보 중 2마리를 골라 함께 싸우고, 레벨업 카드로 최대 4레벨까지 강화합니다."
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

        // 연출 상태. 설명창은 일시정지 중에도 열리므로 시간은 전부 unscaled로 센다.
        private CanvasGroup _panelGroup;
        private Vector3 _panelBaseScale = Vector3.one;
        private float _openElapsed = float.MaxValue;
        private float _closeElapsed;
        private bool _closing;

        private RectTransform _tabRoot;
        private CanvasGroup _tabGroup;
        private Vector2 _tabBasePosition;
        private float _tabElapsed = float.MaxValue;
        private readonly List<RowAnim> _rowAnims = new List<RowAnim>();

        private sealed class RowAnim
        {
            public CanvasGroup group;
            public AugmentGuideEntry augmentEntry;
        }

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (gameplayTabButton != null) gameplayTabButton.onClick.AddListener(ShowGameplay);
            if (augmentTabButton != null) augmentTabButton.onClick.AddListener(ShowAugments);
            if (fairyTabButton != null) fairyTabButton.onClick.AddListener(ShowFairy);
            if (panelRoot != null)
            {
                _panelBaseScale = panelRoot.transform.localScale;
                _panelGroup = GetOrAddGroup(panelRoot);
                panelRoot.SetActive(false);
            }
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
            if (open) TickAnimations(Mathf.Min(Time.unscaledDeltaTime, 0.1f));

            // 설명창을 연 동안과 캐릭터 선택이 끝난 뒤에는 열기 버튼을 숨긴다.
            if (openButton != null)
            {
                bool showButton = !open && (characterSelectPanel == null || characterSelectPanel.activeInHierarchy);
                if (openButton.gameObject.activeSelf != showButton)
                    openButton.gameObject.SetActive(showButton);
            }

            if (!open) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !_closing)
                Close();
        }

        /// <summary>설명창을 열고 게임플레이 탭부터 보여준다.</summary>
        public void Open()
        {
            if (panelRoot == null) return;

            BuildRows();
            _closing = false;
            _openElapsed = 0f;
            ShowGameplay();
            ApplyPanel(GuideAnimLogic.OpenScale(0f), GuideAnimLogic.OpenAlpha(0f));
            panelRoot.SetActive(true);
        }

        /// <summary>설명창을 닫는다. 켜져 있는 동안엔 짧게 줄어들며 사라진 뒤에 꺼진다.</summary>
        public void Close()
        {
            if (panelRoot == null || _closing) return;

            if (!panelRoot.activeInHierarchy)
            {
                panelRoot.SetActive(false);
                return;
            }

            _closing = true;
            _closeElapsed = 0f;
        }

        /// <summary>연출 없이 바로 닫는다.</summary>
        public void CloseImmediate()
        {
            _closing = false;
            _openElapsed = float.MaxValue;
            if (panelRoot != null)
            {
                ApplyPanel(1f, 1f);
                panelRoot.SetActive(false);
            }
        }

        private static CanvasGroup GetOrAddGroup(GameObject target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.AddComponent<CanvasGroup>();
        }

        private void ApplyPanel(float scale, float alpha)
        {
            if (panelRoot == null) return;
            panelRoot.transform.localScale = _panelBaseScale * scale;
            if (_panelGroup != null) _panelGroup.alpha = alpha;
        }

        private void TickAnimations(float dt)
        {
            if (_closing)
            {
                _closeElapsed += dt;
                float closeT = GuideAnimLogic.Progress(_closeElapsed, GuideAnimLogic.CloseDuration);
                ApplyPanel(GuideAnimLogic.CloseScale(closeT), GuideAnimLogic.CloseAlpha(closeT));
                if (closeT >= 1f) CloseImmediate();
                return;
            }

            if (_openElapsed < GuideAnimLogic.OpenDuration)
            {
                _openElapsed += dt;
                float openT = GuideAnimLogic.Progress(_openElapsed, GuideAnimLogic.OpenDuration);
                ApplyPanel(GuideAnimLogic.OpenScale(openT), GuideAnimLogic.OpenAlpha(openT));
            }

            if (_tabElapsed < GuideAnimLogic.TabTotalDuration)
            {
                _tabElapsed += dt;
                ApplyTab(_tabElapsed >= GuideAnimLogic.TabTotalDuration);
            }
        }

        private void ApplyTab(bool finished)
        {
            float t = GuideAnimLogic.Progress(_tabElapsed, GuideAnimLogic.TabDuration);
            if (_tabRoot != null)
            {
                float rise = finished ? 0f : GuideAnimLogic.TabOffset(t);
                _tabRoot.anchoredPosition = _tabBasePosition + new Vector2(0f, -rise);
            }
            if (_tabGroup != null) _tabGroup.alpha = finished ? 1f : GuideAnimLogic.TabAlpha(t);

            for (int i = 0; i < _rowAnims.Count; i++)
            {
                RowAnim row = _rowAnims[i];
                float rowT = finished ? 1f : GuideAnimLogic.RowProgress(_tabElapsed, i);
                if (row.group != null) row.group.alpha = GuideAnimLogic.EaseOut(rowT);
                if (row.augmentEntry != null)
                    row.augmentEntry.SetIconScale(finished ? 1f : GuideAnimLogic.Bounce(rowT));
            }
        }

        // 탭 연출의 대상(내용 루트와 보여줄 줄들)을 정하고 처음 상태로 돌려놓는다.
        private void BeginTabAnimation(GuideTab tab)
        {
            // 이전 탭에서 옮겨 둔 위치와 투명도를 원래대로 되돌린다.
            if (_tabRoot != null) _tabRoot.anchoredPosition = _tabBasePosition;
            if (_tabGroup != null) _tabGroup.alpha = 1f;
            foreach (RowAnim row in _rowAnims)
            {
                if (row.group != null) row.group.alpha = 1f;
                if (row.augmentEntry != null) row.augmentEntry.SetIconScale(1f);
            }
            _rowAnims.Clear();

            GameObject rootObject = GuideTabLogic.ShowsGameplayRoot(tab) ? gameplayRoot : listRoot;
            _tabRoot = rootObject != null ? rootObject.GetComponent<RectTransform>() : null;
            _tabGroup = rootObject != null ? GetOrAddGroup(rootObject) : null;
            if (_tabRoot != null) _tabBasePosition = _tabRoot.anchoredPosition;

            List<GameObject> rows = GuideTabLogic.ShowsAugmentRows(tab) ? _augmentRows
                : GuideTabLogic.ShowsFairyRows(tab) ? _fairyRows : null;
            if (rows != null)
            {
                foreach (GameObject row in rows)
                {
                    if (row == null) continue;
                    _rowAnims.Add(new RowAnim
                    {
                        group = GetOrAddGroup(row),
                        augmentEntry = row.GetComponent<AugmentGuideEntry>()
                    });
                }
            }

            _tabElapsed = 0f;
            ApplyTab(false);
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

            BeginTabAnimation(tab);
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
