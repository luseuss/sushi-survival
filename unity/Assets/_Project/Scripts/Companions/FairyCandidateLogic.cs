using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Companions
{
    /// <summary>
    /// 호감도 대화에서 고를 펫 후보를 정한다. 성향(저돌적/신중/다정)마다 카탈로그 번호 후보를 따로 두고,
    /// 성향이 없으면 균형형 후보를 쓴다. 카탈로그 범위를 벗어나거나 겹치는 번호는 걸러낸다.
    /// </summary>
    public static class FairyCandidateLogic
    {
        /// <summary>한 번에 보여주는 후보 수. 호감도 대화의 선택지 버튼이 최대 3개다.</summary>
        public const int MaxCandidates = 3;

        public static List<int> ForTrait(PlayerTrait trait, int[] bold, int[] careful, int[] kind, int[] neutral,
                                         int catalogSize)
        {
            int[] source = trait switch
            {
                PlayerTrait.Bold => bold,
                PlayerTrait.Careful => careful,
                PlayerTrait.Kind => kind,
                _ => neutral,
            };

            var result = new List<int>();
            if (source == null) return result;

            foreach (int index in source)
            {
                if (index < 0 || index >= catalogSize) continue;
                if (result.Contains(index)) continue;

                result.Add(index);
                if (result.Count >= MaxCandidates) break;
            }

            return result;
        }

        /// <summary>고르는 중인 안내 문구. number는 1부터, total은 모두 몇 마리를 고르는지.</summary>
        public static string PromptText(PlayerTrait trait, int number, int total)
        {
            string lead = trait switch
            {
                PlayerTrait.Bold => "저돌적인 당신과 어울리는 요정들입니다.",
                PlayerTrait.Careful => "신중한 당신과 어울리는 요정들입니다.",
                PlayerTrait.Kind => "다정한 당신과 어울리는 요정들입니다.",
                _ => "당신과 함께할 요정들입니다.",
            };

            return $"{lead}\n함께 싸울 요정을 골라 주세요. ({number}/{total})";
        }
    }
}
