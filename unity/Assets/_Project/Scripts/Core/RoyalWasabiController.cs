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

        public void Show(Sprite portrait, Action onSuccess, Action onComplete)
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

                    if (success)
                        onSuccess?.Invoke();

                    panel.ShowResult(success, portrait, () =>
                    {
                        panel.Hide();
                        onComplete?.Invoke();
                    });
                });
            });
        }
    }
}
