namespace SushiSurvival.Audio
{
    /// <summary>효과음 종류. 코드는 이 이름으로 부르고, 실제 소리 파일은 SoundLibrary에서 이 이름에 연결한다.</summary>
    public enum SfxId
    {
        None = 0,

        // 전투
        WeaponSwing,
        WeaponShoot,
        EnemyHit,
        EnemyDie,
        PlayerHit,
        PlayerDie,

        // 보상
        GemPickup,
        ItemPickup,
        LevelUp,
        Revive,

        // 보스
        BossAppear,
        BossMeteorWarning,
        BossMeteorImpact,
        BossCharge,
        BossSummon,
        BossDie,

        // UI
        ButtonClick,
        ButtonHover,
        PopupOpen,
        CardSelect,
        RpsReveal,
        RpsWin,
        RpsLose,

        // 나중에 추가한 것 — 에셋에 저장된 번호가 밀리지 않도록 맨 끝에 둔다.
        UmbrellaSpin,
        ShotgunShoot,
    }

    /// <summary>배경음악 종류.</summary>
    public enum BgmId
    {
        None = 0,
        Title,
        Story,
        CharacterSelect,
        Battle,
        Boss,
        Victory,
        Defeat,
        RoyalPalace,
    }
}
