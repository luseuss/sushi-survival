using System;
using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Player;
using SushiSurvival.UI;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 레벨업 카드 대신 고르는 도박. 10% 확률로 스탯 4종을 크게 강화하고,
    /// 실패하면 그 레벨업에서 아무것도 못 얻는다 — 보너스가 아니라 대체라서
    /// 위로 보상이 없다.
    ///
    /// 버프 계산은 새로 만들지 않는다. 호감도 대화 #1의 AffinityBuffLogic/
    /// AffinityBuffApplier를 비율만 다르게(대화 12.5% → 이건 50%) 재사용한다.
    /// </summary>
    public class RoyalWasabiController : MonoBehaviour
    {
        [SerializeField] private RoyalWasabiPanel panel;
        [Range(0f, 1f)]
        [SerializeField] private float successChance = 0.1f;
        [Range(0f, 1f)]
        [Tooltip("성공 시 각 증강 maxCap의 이 비율만큼 강화한다.")]
        [SerializeField] private float buffRatio = 0.5f;
        [SerializeField] private AugmentData attackDamageAugment;
        [SerializeField] private AugmentData attackSpeedAugment;
        [SerializeField] private AugmentData moveSpeedAugment;
        [SerializeField] private AugmentData maxHealthAugment;

        private readonly System.Random _random = new System.Random();

        public void Show(PlayerStats stats, PlayerHealth health, Action onComplete)
        {
            if (panel == null)
            {
                Debug.LogError($"{name}: panel이 비어 있어 왕궁 연출을 표시할 수 없습니다.");
                onComplete?.Invoke();
                return;
            }

            bool success = _random.NextDouble() < successChance;

            if (success)
            {
                Apply(attackDamageAugment, stats, health);
                Apply(attackSpeedAugment, stats, health);
                Apply(moveSpeedAugment, stats, health);
                Apply(maxHealthAugment, stats, health);
            }

            panel.Show(success, () =>
            {
                panel.Hide();
                onComplete?.Invoke();
            });
        }

        private void Apply(AugmentData augment, PlayerStats stats, PlayerHealth health)
        {
            if (augment == null)
            {
                Debug.LogError($"{name}: 증강 필드 하나가 비어 있어 그 스탯은 강화되지 않습니다.");
                return;
            }

            float amount = AffinityBuffLogic.GetBuffAmount(augment.maxCap, buffRatio);
            AffinityBuffApplier.Apply(augment, amount, stats, health);
        }
    }
}
