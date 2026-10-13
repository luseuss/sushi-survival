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
    /// "증강"과 "전용 무기" 두 탭이 있는 목록이 나온다. 닫기 버튼이나 ESC로 닫는다.
    /// 목록은 처음 열 때 한 번만 만들고, 탭을 바꾸면 켜고 끄기만 한다.
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
        [SerializeField] private Transform content;

        [Header("증강 탭")]
        [SerializeField] private Button augmentTabButton;
        [Tooltip("설명창에 보여줄 증강. 위에서부터 이 순서대로 나온다.")]
        [SerializeField] private AugmentData[] augments;
        [SerializeField] private AugmentGuideEntry augmentEntryPrefab;

        [Header("전용 무기 탭")]
        [SerializeField] private Button weaponTabButton;
        [SerializeField] private WeaponGuideSection[] weaponSections;
        [SerializeField] private WeaponGuideEntry weaponEntryPrefab;
        [Tooltip("무기 머리말(예: 아델린 — 양산)에 쓰는 글자 줄 프리팹.")]
        [SerializeField] private Text headerPrefab;

        private readonly List<GameObject> _augmentRows = new List<GameObject>();
        private readonly List<GameObject> _weaponRows = new List<GameObject>();
        private bool _built;
        private bool _selectWasVisible;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (augmentTabButton != null) augmentTabButton.onClick.AddListener(ShowAugments);
            if (weaponTabButton != null) weaponTabButton.onClick.AddListener(ShowWeapons);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (augmentTabButton != null) augmentTabButton.onClick.RemoveListener(ShowAugments);
            if (weaponTabButton != null) weaponTabButton.onClick.RemoveListener(ShowWeapons);
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

        public void Open()
        {
            if (panelRoot == null) return;

            BuildRows();
            ShowAugments();
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void ShowAugments() => ShowTab(true);

        public void ShowWeapons() => ShowTab(false);

        private void ShowTab(bool augmentsTab)
        {
            foreach (GameObject row in _augmentRows) row.SetActive(augmentsTab);
            foreach (GameObject row in _weaponRows) row.SetActive(!augmentsTab);

            Tint(augmentTabButton, augmentsTab);
            Tint(weaponTabButton, !augmentsTab);

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

            if (augmentEntryPrefab != null)
            {
                foreach (AugmentData data in augments)
                {
                    if (data == null) continue;
                    AugmentGuideEntry entry = Instantiate(augmentEntryPrefab, content);
                    entry.Bind(data);
                    _augmentRows.Add(entry.gameObject);
                }
            }

            if (weaponEntryPrefab == null) return;

            foreach (WeaponGuideSection section in weaponSections)
            {
                if (section == null || section.weapon == null) continue;

                if (headerPrefab != null)
                {
                    Text header = Instantiate(headerPrefab, content);
                    header.text = section.title;
                    _weaponRows.Add(header.gameObject);
                }

                var steps = WeaponGuideLogic.BuildSteps(section.weapon, section.evolvedWeapon,
                    section.evolveFromLevel, section.evolveNote, section.evolvedIsUmbrella);
                foreach (WeaponGuideStep step in steps)
                {
                    WeaponGuideEntry entry = Instantiate(weaponEntryPrefab, content);
                    entry.Bind(step);
                    _weaponRows.Add(entry.gameObject);
                }
            }
        }
    }
}
