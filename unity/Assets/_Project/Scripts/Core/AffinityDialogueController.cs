using System;
using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Player;
using SushiSurvival.UI;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 호감도 대화 #1(런 시작 직전)·#2(5:00 보스전 진입 직전)의 진입점. 둘 다
    /// 같은 패널·같은 버프 로직을 쓰고 질문만 다르다. 대화 데이터가 없거나
    /// 비어 있으면 즉시 onComplete를 불러 건너뛴다 — 아직 대본이 없는
    /// 캐릭터/시점도 런이 정상적으로 진행돼야 한다.
    /// </summary>
    public class AffinityDialogueController : MonoBehaviour
    {
        [SerializeField] private AffinityDialoguePanel panel;
        [Tooltip("버프 = 증강 maxCap × 이 비율. 기획서 권장 10~15%의 중간값.")]
        [Range(0f, 1f)]
        [SerializeField] private float buffRatio = 0.125f;

        /// <param name="recordBuff">
        /// 적용된 증강·수치를 호출자(LevelSystem)에 되돌려준다. 보스 씬으로 넘어갈 때
        /// 이 버프를 다시 적용할 수 있도록 LevelSystem.RecordExternalBuff를 넘겨받는다 —
        /// 안 넘기면 GameScene→BossScene 전환 시 대화로 받은 버프가 사라진다.
        /// </param>
        public void Show(AffinityDialogueData data, Sprite portrait, PlayerStats stats, PlayerHealth health,
                         Action<AugmentData, float> recordBuff, Action onComplete)
            => ShowQuestion(data?.question1, portrait, stats, health, recordBuff, onComplete);

        public void ShowSecond(AffinityDialogueData data, Sprite portrait, PlayerStats stats, PlayerHealth health,
                         Action<AugmentData, float> recordBuff, Action onComplete)
            => ShowQuestion(data?.question2, portrait, stats, health, recordBuff, onComplete);

        private void ShowQuestion(AffinityDialogueQuestion question, Sprite portrait, PlayerStats stats,
                         PlayerHealth health, Action<AugmentData, float> recordBuff, Action onComplete)
        {
            if (question == null || question.choices == null || question.choices.Length == 0)
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

            panel.Show(portrait, question, choice =>
            {
                if (choice.augment != null)
                {
                    float amount = AffinityBuffLogic.GetBuffAmount(choice.augment.maxCap, buffRatio);
                    AffinityBuffApplier.Apply(choice.augment, amount, stats, health);
                    recordBuff?.Invoke(choice.augment, amount);
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
