namespace SushiSurvival.Weapons
{
    public static class ProjectileLogic
    {
        /// <summary>
        /// 관통 수의 의미는 기획서를 따른다 — 관통 0은 "1체만 타격 후 소멸".
        /// 즉 적중 수가 관통 수를 넘어서면 사라진다.
        /// </summary>
        public static bool ShouldDespawn(int enemiesHit, int pierceCount)
            => enemiesHit > pierceCount;
    }
}
