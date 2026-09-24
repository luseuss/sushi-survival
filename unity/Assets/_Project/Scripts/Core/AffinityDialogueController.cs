using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using SushiSurvival.Data;
using SushiSurvival.Player;
using SushiSurvival.UI;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 호감도 대화 #1(런 시작 직전)·#2(5:00 보스전 진입 직전)의 진입점.
    /// #1은 introLines(자기소개 나레이션) → question1(증강 3택) 순으로,
    /// #2는 bossIntroLines(나레이션만)만 재생한다. 대화 데이터가 없거나
    /// 줄이 비어 있으면 즉시 다음 단계로 건너뛴다 — 아직 대본이 없는
    /// 캐릭터/시점도 런이 정상적으로 진행돼야 한다.
    /// </summary>
    public class AffinityDialogueController : MonoBehaviour
    {
        [SerializeField] private AffinityDialoguePanel panel;
        [Tooltip("introLines/bossIntroLines를 재생하는 나레이션 패널(StoryScene에서 만든 것과 같은 컴포넌트).")]
        [SerializeField] private StoryPanel introPanel;
        [Tooltip("버프 = 증강 maxCap × 이 비율. 기획서 권장 10~15%의 중간값.")]
        [Range(0f, 1f)]
        [SerializeField] private float buffRatio = 0.125f;

        private Coroutine _lineRoutine;

        /// <param name="recordBuff">
        /// 적용된 증강·수치를 호출자(LevelSystem)에 되돌려준다. 보스 씬으로 넘어갈 때
        /// 이 버프를 다시 적용할 수 있도록 LevelSystem.RecordExternalBuff를 넘겨받는다 —
        /// 안 넘기면 GameScene→BossScene 전환 시 대화로 받은 버프가 사라진다.
        /// </param>
        public void Show(AffinityDialogueData data, Sprite portrait, Sprite standing, PlayerStats stats, PlayerHealth health,
                         Action<AugmentData, float> recordBuff, Action onComplete)
        {
            PlayLines(data?.introLines,
                () => ShowQuestion(data?.question1, portrait, standing, stats, health, recordBuff, onComplete));
        }

        /// <summary>보스전 직전 나레이션만 재생한다. 증강 선택은 없다.</summary>
        public void ShowSecond(AffinityDialogueData data, Sprite portrait, PlayerStats stats, PlayerHealth health,
                         Action<AugmentData, float> recordBuff, Action onComplete)
        {
            PlayLines(data?.bossIntroLines, onComplete);
        }

        private void PlayLines(StoryLine[] lines, Action onDone)
        {
            if (lines == null || lines.Length == 0 || introPanel == null)
            {
                onDone?.Invoke();
                return;
            }

            if (_lineRoutine != null) StopCoroutine(_lineRoutine);
            _lineRoutine = StartCoroutine(PlayLinesRoutine(lines, onDone));
        }

        private IEnumerator PlayLinesRoutine(StoryLine[] lines, Action onDone)
        {
            introPanel.gameObject.SetActive(true);

            int index = 0;
            introPanel.ShowLine(lines[index]);

            while (!StoryDialogueLogic.IsFinished(index, lines.Length))
            {
                yield return null;

                if (!AdvancePressed()) continue;

                index = StoryDialogueLogic.NextIndex(index, lines.Length);
                if (StoryDialogueLogic.IsFinished(index, lines.Length)) break;

                introPanel.ShowLine(lines[index]);
            }

            introPanel.gameObject.SetActive(false);
            _lineRoutine = null;
            onDone?.Invoke();
        }

        private bool AdvancePressed()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
        }

        private void ShowQuestion(AffinityDialogueQuestion question, Sprite portrait, Sprite standing, PlayerStats stats,
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

            panel.Show(portrait, standing, question, choice =>
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
