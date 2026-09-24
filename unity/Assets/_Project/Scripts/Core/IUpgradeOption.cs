using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 레벨업 3택에 오르는 선택지. 증강과 무기 강화가 같은 풀에서 뽑히도록
    /// 하나의 인터페이스로 묶는다.
    /// </summary>
    public interface IUpgradeOption
    {
        string DisplayName { get; }
        /// <summary>카드 제목 아래에 적는 설명. 비어 있으면 카드에는 제목만 보인다.</summary>
        string Description { get; }
        Sprite Icon { get; }
        void Apply();
    }
}
