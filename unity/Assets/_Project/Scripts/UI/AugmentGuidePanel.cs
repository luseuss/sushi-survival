using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
using SushiSurvival.Companions;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 캐릭터 선택 화면 위에 띄우는 설명창. 열기 버튼은 캐릭터 선택 화면이 떠 있는 동안만 보이고, 누르면
    /// "게임플레이", "증강", "와사비·요정" 세 탭이 있는 두루마리 설명창이 나온다. 닫기 버튼이나 ESC로 닫는다.
    /// - 게임플레이: 씬에 직접 쓴 내용(조작·한 줄 요약 등)과 오른쪽 영상 박스.
    /// - 증강 / 와사비·요정: 왼쪽 4×4 칸에 항목이 늘어서고, 칸에 마우스를 올리면(또는 방향키·클릭) 오른쪽 영상 박스와
    ///   설명이 그 항목으로 바뀐다. 격자 화면은 GuideBoard가 런타임에 만든다.
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
        [Tooltip("게임플레이 탭의 영상 박스에 재생할 영상. 비우면 검은 바탕만 보인다.")]
        [SerializeField] private VideoClip gameplayVideo;

        [Header("격자 화면 (증강·와사비·요정 탭)")]
        [Tooltip("옛 스크롤 목록 오브젝트. 새 격자 화면을 쓰므로 항상 꺼 둔다. 이 오브젝트의 부모(두루마리)에 격자를 만든다.")]
        [SerializeField] private GameObject listRoot;
        [SerializeField] private Sprite slotFrameSprite;
        [SerializeField] private Sprite slotFrameSelectedSprite;
        [SerializeField] private Sprite videoFrameSprite;
        [SerializeField] private GuideLayout layout = new GuideLayout();

        [Header("증강 탭")]
        [Tooltip("설명창에 보여줄 증강. 왼쪽 위 칸부터 이 순서대로 놓인다.")]
        [SerializeField] private AugmentData[] augments;

        [Header("와사비·요정 탭")]
        [Tooltip("와사비 도전 칸에 나오는 설명. 한 줄에 한 항목.")]
        [TextArea(1, 3)]
        [SerializeField] private string[] fairyIntroLines =
        {
            "레벨업 카드 대신 4번째 버튼으로 왕에게 와사비를 빌러 갈 수 있습니다.",
            "가위바위보에서 이기면 무기가 그 자리에서 변신하고 공격력이 25% 더 붙습니다. 지면 그 레벨업의 보상은 없습니다.",
            "무기는 강화 3레벨에서도 저절로 변신하지만, 와사비로 일찍 변신한 무기가 더 강합니다. 이미 변신했다면 와사비 버튼은 나오지 않습니다.",
            "요정은 시작 대화에서 성향에 맞는 후보 중 2마리를 골라 함께 싸우고, 레벨업 카드로 최대 4레벨까지 강화합니다."
        };
        [Tooltip("와사비 도전 칸의 제목.")]
        [SerializeField] private string wasabiGuideTitle = "와사비 도전";
        [Tooltip("와사비 도전 칸의 아이콘. 비우면 아이콘 없이 나온다.")]
        [SerializeField] private Sprite wasabiGuideIcon;
        [Tooltip("소개할 펫 종류. FairyController의 카탈로그와 같은 에셋을 같은 순서로 넣는다.")]
        [SerializeField] private FairyData[] fairyCatalog;

        private GuideBoard _board;
        private readonly List<GuideItem> _augmentItems = new List<GuideItem>();
        private readonly List<GuideItem> _fairyItems = new List<GuideItem>();
        private bool _selectWasVisible;

        // 연출 상태. 설명창은 일시정지 중에도 열리므로 시간은 전부 unscaled로 센다.
        private CanvasGroup _panelGroup;
        private Vector3 _panelBaseScale = Vector3.one;
        private float _openElapsed = float.MaxValue;
        private float _closeElapsed;
        private bool _closing;

        private float _tabElapsed = float.MaxValue;
        private readonly List<TabTarget> _tabTargets = new List<TabTarget>();

        private sealed class TabTarget
        {
            public RectTransform rect;
            public CanvasGroup group;
            public Vector2 basePosition;
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

            if (!open || _closing) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            HandleNavigation(keyboard);
        }

        // 방향키·게임패드 십자키로 격자의 칸을 옮긴다(마우스 없이도 볼 수 있게).
        private void HandleNavigation(Keyboard keyboard)
        {
            if (_board == null) return;

            int dx = 0, dy = 0;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame) dx = -1;
                else if (keyboard.rightArrowKey.wasPressedThisFrame) dx = 1;
                else if (keyboard.upArrowKey.wasPressedThisFrame) dy = -1;
                else if (keyboard.downArrowKey.wasPressedThisFrame) dy = 1;
            }

            Gamepad pad = Gamepad.current;
            if (pad != null && dx == 0 && dy == 0)
            {
                if (pad.dpad.left.wasPressedThisFrame) dx = -1;
                else if (pad.dpad.right.wasPressedThisFrame) dx = 1;
                else if (pad.dpad.up.wasPressedThisFrame) dy = -1;
                else if (pad.dpad.down.wasPressedThisFrame) dy = 1;
            }

            if (dx != 0 || dy != 0) _board.Navigate(dx, dy);
        }

        /// <summary>설명창을 열고 게임플레이 탭부터 보여준다.</summary>
        public void Open()
        {
            if (panelRoot == null) return;

            EnsureBoard();
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
            if (_board != null) _board.StopVideo();
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

            if (_tabElapsed < GuideAnimLogic.TabDuration)
            {
                _tabElapsed += dt;
                ApplyTab(_tabElapsed >= GuideAnimLogic.TabDuration);
            }
        }

        // 탭 내용(게임플레이 내용 / 격자 화면)이 아래에서 올라오며 나타난다.
        private void ApplyTab(bool finished)
        {
            float t = GuideAnimLogic.Progress(_tabElapsed, GuideAnimLogic.TabDuration);
            float rise = finished ? 0f : GuideAnimLogic.TabOffset(t);
            float alpha = finished ? 1f : GuideAnimLogic.TabAlpha(t);

            foreach (TabTarget target in _tabTargets)
            {
                if (target.rect != null) target.rect.anchoredPosition = target.basePosition + new Vector2(0f, -rise);
                if (target.group != null) target.group.alpha = alpha;
            }
        }

        private void BeginTabAnimation(GuideTab tab)
        {
            // 이전 탭에서 옮겨 둔 위치와 투명도를 원래대로 되돌린다.
            foreach (TabTarget target in _tabTargets)
            {
                if (target.rect != null) target.rect.anchoredPosition = target.basePosition;
                if (target.group != null) target.group.alpha = 1f;
            }
            _tabTargets.Clear();

            AddTabTarget(gameplayRoot != null && GuideTabLogic.ShowsGameplayRoot(tab) ? gameplayRoot.GetComponent<RectTransform>() : null);
            AddTabTarget(_board != null ? _board.Root : null);

            _tabElapsed = 0f;
            ApplyTab(false);
        }

        private void AddTabTarget(RectTransform rect)
        {
            if (rect == null) return;

            _tabTargets.Add(new TabTarget
            {
                rect = rect,
                group = GetOrAddGroup(rect.gameObject),
                basePosition = rect.anchoredPosition
            });
        }

        public void ShowGameplay() => ShowTab(GuideTab.Gameplay);

        public void ShowAugments() => ShowTab(GuideTab.Augments);

        public void ShowFairy() => ShowTab(GuideTab.Fairy);

        private void ShowTab(GuideTab tab)
        {
            EnsureBoard();

            if (gameplayRoot != null) gameplayRoot.SetActive(GuideTabLogic.ShowsGameplayRoot(tab));
            // 옛 스크롤 목록은 새 격자 화면으로 대체돼 항상 꺼 둔다.
            if (listRoot != null) listRoot.SetActive(false);

            Tint(gameplayTabButton, tab == GuideTab.Gameplay);
            Tint(augmentTabButton, tab == GuideTab.Augments);
            Tint(fairyTabButton, tab == GuideTab.Fairy);

            if (_board != null)
            {
                switch (tab)
                {
                    case GuideTab.Gameplay: _board.ShowVideoOnly(gameplayVideo); break;
                    case GuideTab.Augments: _board.ShowGrid(_augmentItems); break;
                    default: _board.ShowGrid(_fairyItems); break;
                }
            }

            BeginTabAnimation(tab);
        }

        private static void Tint(Button button, bool selected)
        {
            if (button != null && button.TryGetComponent<Image>(out var image))
                image.color = selected ? SelectedTabColor : IdleTabColor;
        }

        // 격자 화면은 처음 열 때 한 번만 만든다. 두루마리(옛 목록의 부모) 위에 깔린다.
        private void EnsureBoard()
        {
            if (_board != null) return;

            Transform parent = listRoot != null && listRoot.transform.parent != null
                ? listRoot.transform.parent
                : (panelRoot != null ? panelRoot.transform : transform);

            _board = GuideBoard.Create(parent, ResolveFont(), slotFrameSprite, slotFrameSelectedSprite,
                                       videoFrameSprite, layout);
            BuildItems();
        }

        private Font ResolveFont()
        {
            Text sample = closeButton != null ? closeButton.GetComponentInChildren<Text>(true) : null;
            if (sample != null && sample.font != null) return sample.font;

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void BuildItems()
        {
            _augmentItems.Clear();
            _fairyItems.Clear();

            if (augments != null)
            {
                foreach (AugmentData data in augments)
                {
                    if (data == null) continue;

                    string effect = string.IsNullOrEmpty(data.description)
                        ? UpgradeDescriptionLogic.DescribeAugment(data.statType, data.valuePerPick)
                        : data.description;
                    string limit = UpgradeDescriptionLogic.DescribeAugmentLimit(data.statType, data.valuePerPick, data.maxCap);

                    _augmentItems.Add(new GuideItem
                    {
                        title = data.augmentName,
                        body = string.IsNullOrEmpty(limit) ? effect : effect + "\n" + limit,
                        icon = data.icon,
                        video = data.guideVideo
                    });
                }
            }

            // 와사비 도전 설명이 첫 칸에 오고, 그 뒤로 펫이 이어진다.
            _fairyItems.Add(new GuideItem
            {
                title = wasabiGuideTitle,
                body = fairyIntroLines != null ? string.Join("\n", fairyIntroLines) : string.Empty,
                icon = wasabiGuideIcon
            });

            if (fairyCatalog != null)
            {
                foreach (FairyData data in fairyCatalog)
                {
                    if (data == null) continue;

                    string stats = data.levels != null && data.levels.Length > 0
                        ? FairyDescriptionLogic.DescribeStats(data.levels[0])
                        : string.Empty;

                    _fairyItems.Add(new GuideItem
                    {
                        title = data.displayName,
                        body = string.IsNullOrEmpty(stats) ? data.roleLine : data.roleLine + "\n" + stats,
                        frames = data.frames,
                        framesPerSecond = data.framesPerSecond,
                        video = data.guideVideo
                    });
                }
            }
        }
    }
}
