using System;
using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Player;
using SushiSurvival.UI;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 호감도 대화 #1의 진입점. GameManager가 캐릭터 스폰 직후 이걸 부른다.
    /// 대화 데이터가 없거나 비어 있으면 즉시 onComplete를 불러 건너뛴다 —
    /// 아직 대본이 없는 캐릭터도 런이 정상적으로 진행돼야 한다.
    /// </summary>
    public class AffinityDialogueController : MonoBehaviour
    {
        [SerializeField] private AffinityDialoguePanel panel;
        [Tooltip("버프 = 증강 maxCap × 이 비율. 기획서 권장 10~15%의 중간값.")]
        [Range(0f, 1f)]
        [SerializeField] private float buffRatio = 0.125f;

        public void Show(AffinityDialogueData data, Sprite portrait,
                         PlayerStats stats, PlayerHealth health, Action onComplete)
        {
            if (data == null || data.question1 == null ||
                data.question1.choices == null || data.question1.choices.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            if (panel == null)
            {
                Debug.LogError($"{name}: panel이 비어 있어 대화를 표시할 수 없습니다.");
                onComplete?.Invoke();
                return;
            }

            panel.Show(portrait, data.question1, choice =>
            {
                if (choice.augment != null)
                {
                    float amount = AffinityBuffLogic.GetBuffAmount(choice.augment.maxCap, buffRatio);
                    AffinityBuffApplier.Apply(choice.augment, amount, stats, health);
                }
                else
                {
                    Debug.LogWarning($"{name}: 선택지 '{choice.choiceText}'에 augment가 연결되지 않아 버프 없이 넘어갑니다.");
                }

                panel.Hide();
                onComplete?.Invoke();
            });
        }
    }
}
