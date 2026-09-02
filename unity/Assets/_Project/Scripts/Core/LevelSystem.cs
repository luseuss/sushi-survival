using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Player;
using SushiSurvival.UI;
using SushiSurvival.Weapons;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 경험치 누적 → 레벨업 → 3택 팝업 → 적용까지를 관장한다.
    /// 황금 젬으로 여러 레벨이 한 번에 오를 수 있으므로 대기 큐를 둔다.
    /// </summary>
    public class LevelSystem : MonoBehaviour
    {
        private const int OptionCount = 3;

        [SerializeField] private LevelUpPanel panel;
        [SerializeField] private RoyalWasabiController royalWasabiController;
        [SerializeField] private AugmentData[] augments;
        [Tooltip("무기 강화 선택지에 쓸 아이콘. 비워도 동작한다.")]
        [SerializeField] private Sprite weaponUpgradeIcon;
        [Tooltip("Lv1에서 다음 레벨까지 필요한 경험치.")]
        [SerializeField] private float baseXp = 5f;
        [Tooltip("레벨이 오를 때마다 필요 경험치에 더해지는 값.")]
        [SerializeField] private float xpIncrementPerLevel = 3f;

        public int CurrentLevel { get; private set; } = 1;

        private readonly Dictionary<AugmentData, float> _accumulated = new Dictionary<AugmentData, float>();
        private readonly List<AugmentData> _pickedAugments = new List<AugmentData>();

        public IReadOnlyList<AugmentData> PickedAugments => _pickedAugments;

        /// <summary>
        /// 팝업이 열려 있거나 아직 못 띄운 레벨업이 남아 있으면 true.
        /// BossDirector가 등장 연출을 시작하기 전에 이걸로 기다린다 — 팝업은
        /// timeScale 0이고 등장 연출은 0.3이라, 겹치면 팝업이 닫히면서
        /// CloseAndResume()이 timeScale을 1로 되돌려 연출이 깨진다.
        /// </summary>
        public bool IsShowingPopup => _panelOpen || _pendingLevelUps > 0;
        private readonly System.Random _random = new System.Random();

        private PlayerStats _playerStats;
        private PlayerHealth _playerHealth;
        private WeaponBase _weapon;

        private float _xpTowardNext;
        private int _pendingLevelUps;
        private bool _panelOpen;

        public void SetPlayer(PlayerStats stats, PlayerHealth health, WeaponBase weapon)
        {
            _playerStats = stats;
            _playerHealth = health;
            _weapon = weapon;
        }

        public void AddExperience(float amount)
        {
            _xpTowardNext += amount;

            var progress = LevelCurve.Resolve(_xpTowardNext, CurrentLevel, baseXp, xpIncrementPerLevel);
            _xpTowardNext = progress.XpTowardNext;

            if (progress.LevelsGained <= 0) return;

            CurrentLevel += progress.LevelsGained;
            _pendingLevelUps += progress.LevelsGained;
            Debug.Log($"[LevelSystem] 레벨업! 현재 Lv{CurrentLevel} (대기 {_pendingLevelUps})");

            if (!_panelOpen)
                ShowNext();
        }

        private void ShowNext()
        {
            while (_pendingLevelUps > 0)
            {
                _pendingLevelUps--;

                List<IUpgradeOption> options = BuildOptions();
                if (options.Count == 0)
                {
                    // 모든 증강이 최대치이고 무기도 4강이면 고를 게 없다.
                    // 빈 팝업으로 게임이 멈추지 않도록 조용히 소비한다.
                    continue;
                }

                _panelOpen = true;
                Time.timeScale = 0f;
                panel.Show(options, OnOptionChosen, HandleRoyalWasabiRequested);
                return;
            }

            CloseAndResume();
        }

        /// <summary>
        /// "와사비를 하사받으러 간다"를 눌렀을 때. _panelOpen은 여기서 건드리지
        /// 않는다 — 왕궁 연출이 끝나기 전까지 게임이 재개되면 안 되기 때문이다.
        /// CloseAndResume()에서만 false로 돌아간다.
        /// </summary>
        private void HandleRoyalWasabiRequested()
        {
            panel.Hide();

            if (royalWasabiController == null)
            {
                Debug.LogError($"{name}: royalWasabiController가 비어 있어 도박을 진행할 수 없습니다.");
                ShowNext();
                return;
            }

            royalWasabiController.Show(_playerStats, _playerHealth, ShowNext);
        }

        private void OnOptionChosen(IUpgradeOption option)
        {
            option.Apply();

            if (option is AugmentOption augmentOption)
            {
                var data = augmentOption.Data;
                _accumulated.TryGetValue(data, out float current);
                _accumulated[data] = current + data.valuePerPick;
                _pickedAugments.Add(data);
            }

            panel.Hide();
            _panelOpen = false;

            ShowNext();
        }

        private void CloseAndResume()
        {
            panel.Hide();
            _panelOpen = false;
            Time.timeScale = 1f;
        }

        private List<IUpgradeOption> BuildOptions()
        {
            var candidates = new List<IUpgradeOption>();

            if (_weapon != null && _weapon.CanLevelUp)
                candidates.Add(new WeaponLevelUpOption(_weapon, weaponUpgradeIcon));

            foreach (var augment in augments)
            {
                if (augment == null) continue;

                _accumulated.TryGetValue(augment, out float current);
                if (!AugmentAvailability.IsAvailable(current, augment.maxCap)) continue;

                candidates.Add(new AugmentOption(augment, _playerStats, _playerHealth));
            }

            return UpgradePicker.PickDistinct(candidates, OptionCount, _random);
        }
    }
}
