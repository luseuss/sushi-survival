using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>기존 대사 끝에 대표 성향의 한 줄을 붙인다.</summary>
    public static class TraitLineLogic
    {
        /// <summary>
        /// baseLines가 한 줄 이상이고 dominant의 줄이 있으면 끝에 그 한 줄을 붙인 새 배열을, 아니면 baseLines를
        /// 그대로 돌려준다. 기존 대사가 없으면 성향 한 줄만 단독으로 재생하지 않는다(대화 없이 바로 전투하는
        /// 캐릭터에 갑자기 대화가 생기지 않게).
        /// </summary>
        public static StoryLine[] AppendTraitLine(StoryLine[] baseLines, IReadOnlyList<TraitLine> traitLines,
                                                  PlayerTrait dominant)
        {
            if (baseLines == null || baseLines.Length == 0 || traitLines == null || dominant == PlayerTrait.None)
                return baseLines;

            for (int i = 0; i < traitLines.Count; i++)
            {
                TraitLine entry = traitLines[i];
                if (entry == null || entry.trait != dominant || entry.line == null) continue;

                var result = new StoryLine[baseLines.Length + 1];
                baseLines.CopyTo(result, 0);
                result[baseLines.Length] = entry.line;
                return result;
            }

            return baseLines;
        }
    }
}
