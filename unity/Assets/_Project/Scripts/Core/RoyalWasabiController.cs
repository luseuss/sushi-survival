using System;
using UnityEngine;
using SushiSurvival.UI;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 레벨업 카드 대신 고르는 도박의 연출(대사→가위바위보→결과 공개)만 맡는다.
    /// 성공/완료 시 무엇을 할지는 모른다 — 캐릭터마다 보상이 달라서(아델린은
    /// 무기 교체, 그 외는 스탯 버프) 호출자(LevelSystem)가 콜백으로 정한다.
    /// </summary>
    public class RoyalWasabiController : MonoBehaviour
    {
        [SerializeField] private RockPaperScissorsPanel rpsPanel;
        [SerializeField] private RoyalWasabiPanel panel;
        [Tooltip("가위바위보 손을 공개하는 순간 멈추는 시간(초).")]
        [SerializeField] private float revealHitstopDuration = 0.05f;

        /// <param name="onSuccess">
        /// 성공했을 때만 호출된다. 실제 보상을 적용하고, 결과 화면에 한 줄씩 보여줄 설명 목록을
        /// 돌려준다(승리 연출용). 실패하면 아예 호출되지 않는다 — 위로 보상이 없기 때문이다.
        /// </param>
        public void Show(Sprite portrait, Func<string[]> onSuccess, Action onComplete)
        {
            if (panel == null || rpsPanel == null)
            {
                Debug.LogError($"{name}: panel 또는 rpsPanel이 비어 있어 왕궁 연출을 표시할 수 없습니다.");
                onComplete?.Invoke();
                return;
            }

            // "와사비를 받으러 왔습니다" 대사를 먼저 보여준 뒤에야 가위바위보로
            // 넘어간다 — 결과 확인용 패널을 도입부 연출로도 재사용한다.
            panel.ShowFlavor(portrait, () =>
            {
                // panel.Hide()가 아니라 HideDialogueBox() — 왕궁 배경은
                // 가위바위보 도중에도 계속 보여야 한다.
                panel.HideDialogueBox();

                rpsPanel.Show(success =>
                {
                    rpsPanel.Hide();

                    string[] appliedLines = success ? onSuccess?.Invoke() : null;

                    panel.ShowResult(success, portrait, appliedLines, () =>
                    {
                        panel.Hide();
                        onComplete?.Invoke();
                    });
                }, HandleReveal);
            });
        }

        /// <summary>가위바위보 손을 공개하는 순간마다 히트스톱+화면 번쩍임을 건다.</summary>
        private void HandleReveal()
        {
            if (JuiceDirector.Instance != null)
                JuiceDirector.Instance.Hitstop(revealHitstopDuration);

            panel.FlashReveal();
        }
    }
}
