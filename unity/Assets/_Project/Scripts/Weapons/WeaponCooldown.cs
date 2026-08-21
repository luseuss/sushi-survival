namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 무기 쿨타임 타이머. 생성 직후에는 준비 상태라 첫 공격이 즉시 나간다.
    /// </summary>
    public class WeaponCooldown
    {
        private float _remaining;

        public bool IsReady => _remaining <= 0f;

        public void Tick(float deltaTime) => _remaining -= deltaTime;

        public void Reset(float cooldownSeconds) => _remaining = cooldownSeconds;
    }
}
