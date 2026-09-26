using NUnit.Framework;
using SushiSurvival.Enemies.Boss;

namespace SushiSurvival.EditModeTests
{
    public class ChargeEffectsLogicTests
    {
        [Test]
        public void ChargeDistance_PhaseOneValues()
        {
            // 이동속도 1.6 × 속도배율 4.5 × 돌진 0.55초
            Assert.AreEqual(3.96f, ChargeEffectsLogic.ChargeDistance(1.6f, 4.5f, 0.55f), 0.01f);
        }

        [Test]
        public void ChargeDistance_PhaseTwoValues()
        {
            Assert.AreEqual(5.28f, ChargeEffectsLogic.ChargeDistance(1.6f, 5.5f, 0.6f), 0.01f);
        }

        [Test]
        public void ChargeDistance_NegativeInputs_ClampToZero()
        {
            Assert.AreEqual(0f, ChargeEffectsLogic.ChargeDistance(1.6f, 4.5f, -1f), 0.001f);
            Assert.AreEqual(0f, ChargeEffectsLogic.ChargeDistance(-1f, 4.5f, 0.55f), 0.001f);
        }

        [Test]
        public void LockDelay_SubtractsLockFromWindup()
        {
            Assert.AreEqual(0.65f, ChargeEffectsLogic.LockDelay(0.8f, 0.15f), 0.001f);
            Assert.AreEqual(0.45f, ChargeEffectsLogic.LockDelay(0.6f, 0.15f), 0.001f);
        }

        [Test]
        public void LockDelay_ZeroLock_EqualsWindup()
        {
            Assert.AreEqual(0.8f, ChargeEffectsLogic.LockDelay(0.8f, 0f), 0.001f);
        }

        [Test]
        public void LockDelay_LockLongerThanWindup_ClampsToZero()
        {
            Assert.AreEqual(0f, ChargeEffectsLogic.LockDelay(0.1f, 0.5f), 0.001f);
        }

        [Test]
        public void LockDelay_NegativeLock_IsTreatedAsZero()
        {
            Assert.AreEqual(0.8f, ChargeEffectsLogic.LockDelay(0.8f, -1f), 0.001f);
        }
    }
}
