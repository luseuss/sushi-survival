using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>연습 플레이의 단계. 순서대로 진행한다.</summary>
    public enum TutorialStep
    {
        Move,
        Attack,
        LevelUp,
        Wasabi,
        Skill,
        Done
    }

    /// <summary>
    /// 연습 플레이 단계의 순수 계산부 — 다음 단계, 단계 번호, 안내 글, 이동 목표 도달 판정.
    /// 장면에 적용하는 일은 TutorialDirector가 한다.
    /// </summary>
    public static class TutorialStepLogic
    {
        /// <summary>안내 글에 보이는 단계 수(이동·공격·레벨업·와사비·스킬).</summary>
        public const int TotalSteps = 5;

        /// <summary>공격 단계에서 처치해야 하는 적 수.</summary>
        public const int KillGoal = 3;

        /// <summary>이동 단계의 목표 표시에 닿았다고 보는 거리.</summary>
        public const float MarkerRadius = 1.2f;

        public static TutorialStep Next(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.Move: return TutorialStep.Attack;
                case TutorialStep.Attack: return TutorialStep.LevelUp;
                case TutorialStep.LevelUp: return TutorialStep.Wasabi;
                case TutorialStep.Wasabi: return TutorialStep.Skill;
                default: return TutorialStep.Done;
            }
        }

        /// <summary>1부터 시작하는 단계 번호. 끝(Done)은 마지막 단계 번호와 같다.</summary>
        public static int Number(TutorialStep step) => step == TutorialStep.Done ? TotalSteps : (int)step + 1;

        /// <summary>목표 표시 둘레(radius) 안에 들어왔는지.</summary>
        public static bool ReachedMarker(Vector2 player, Vector2 marker, float radius)
            => (player - marker).sqrMagnitude <= radius * radius;

        /// <summary>처치 수가 목표에 닿았는지.</summary>
        public static bool KillGoalReached(int kills) => kills >= KillGoal;

        /// <summary>단계별 안내 글. 공격 단계는 처치 수(kills)를 보여 준다.</summary>
        public static string Instruction(TutorialStep step, int kills)
        {
            switch (step)
            {
                case TutorialStep.Move:
                    return "WASD로 움직여 보세요. 표시된 곳까지 가 보세요!";
                case TutorialStep.Attack:
                    return $"무기는 알아서 공격해요! 다가오는 초밥을 처치해 보세요 ({Mathf.Clamp(kills, 0, KillGoal)}/{KillGoal})";
                case TutorialStep.LevelUp:
                    return "레벨업! 카드를 하나 골라 보세요.";
                case TutorialStep.Wasabi:
                    return "한 번 더 레벨업! 카드 대신 '와사비' 버튼을 눌러 왕을 만나 보세요.\n가위바위보에 이기면 와사비의 힘으로 더 강력한 무기를 얻어요.";
                case TutorialStep.Skill:
                    return "마우스 클릭으로 우산 스킬을 써 보세요!\n체력바 아래 막대가 쿨타임이에요.";
                default:
                    return "연습 끝! 이제 진짜 게임을 시작해요.";
            }
        }
    }
}
