namespace SushiSurvival.Data
{
    /// <summary>
    /// 세계관 대화에서 쌓이는 성향. None이 0이라, 이 필드가 새로 생겨도 기존 에셋은 자동으로 None이 된다.
    /// 화면에 보이는 이름(용감함·신중함·다정함)은 PlayerTraitLines 데이터에 있다.
    /// </summary>
    public enum PlayerTrait
    {
        None = 0,
        Bold,
        Careful,
        Kind
    }
}
