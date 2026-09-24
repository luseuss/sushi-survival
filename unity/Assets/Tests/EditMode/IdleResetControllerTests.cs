using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class IdleResetControllerTests
    {
        private IdleContext _context;
        private int _resets;
        private IdleTimeouts _timeouts;
        private IdleResetController _controller;

        [SetUp]
        public void SetUp()
        {
            _context = IdleContext.Menu;
            _resets = 0;
            _timeouts = new IdleTimeouts(60f, 60f, 30f);
            _controller = new IdleResetController(() => _context, () => _timeouts, () => _resets++);
        }

        private void Idle(float seconds, float step = 1f)
        {
            for (float t = 0f; t < seconds - 0.0001f; t += step)
                _controller.Tick(step, false);
        }

        [Test]
        public void NoInputForTheTimeout_ResetsExactlyOnce()
        {
            Idle(59f);
            Assert.AreEqual(0, _resets);

            Idle(1f);
            Assert.AreEqual(1, _resets);
        }

        [Test]
        public void AfterReset_TheCountStartsOver()
        {
            Idle(60f);
            Assert.AreEqual(1, _resets);
            Assert.AreEqual(0f, _controller.IdleSeconds, 0.0001f);

            Idle(59f);
            Assert.AreEqual(1, _resets);
        }

        [Test]
        public void AnyInput_RestartsTheCount()
        {
            Idle(50f);
            _controller.Tick(1f, true);
            Idle(50f);

            Assert.AreEqual(0, _resets);
        }

        [Test]
        public void ResultContext_UsesTheShorterTimeout()
        {
            _context = IdleContext.Result;

            Idle(29f);
            Assert.AreEqual(0, _resets);

            Idle(1f);
            Assert.AreEqual(1, _resets);
        }

        [Test]
        public void ContextChange_DoesNotCarryOverIdleTime()
        {
            // 전투 중 55초 가만히 있다가 결과 화면이 떴을 때, 그 55초가 결과 화면의 30초 제한에 합산돼
            // 결과를 보자마자 리셋되면 안 된다.
            _context = IdleContext.Playing;
            Idle(55f);

            _context = IdleContext.Result;
            _controller.Tick(1f, false);

            Assert.AreEqual(0, _resets);
            Assert.AreEqual(1f, _controller.IdleSeconds, 0.0001f);
        }

        [Test]
        public void IgnoredContext_NeverResets()
        {
            _context = IdleContext.Ignore;

            Idle(5000f, 10f);

            Assert.AreEqual(0, _resets);
            Assert.AreEqual(0f, _controller.IdleSeconds, 0.0001f);
        }

        [Test]
        public void ZeroTimeout_DisablesResetForThatContext()
        {
            _timeouts = new IdleTimeouts(0f, 60f, 30f);
            _context = IdleContext.Menu;

            Idle(1000f, 10f);

            Assert.AreEqual(0, _resets);
        }

        [Test]
        public void NotifyActivity_ClearsTheCount()
        {
            Idle(50f);
            _controller.NotifyActivity();
            Idle(50f);

            Assert.AreEqual(0, _resets);
        }

        [Test]
        public void TimeoutChangedAtRuntime_IsPickedUpImmediately()
        {
            Idle(20f);
            _timeouts = new IdleTimeouts(20f, 60f, 30f);
            _controller.Tick(1f, false);

            Assert.AreEqual(1, _resets);
        }

        [Test]
        public void NegativeDt_IsTreatedAsNoTimePassing()
        {
            Idle(30f);
            _controller.Tick(-5f, false);

            Assert.AreEqual(0, _resets);
            Assert.AreEqual(0f, _controller.IdleSeconds, 0.0001f);
        }
    }
}
