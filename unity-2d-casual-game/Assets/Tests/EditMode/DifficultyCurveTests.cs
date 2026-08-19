using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class DifficultyCurveTests
    {
        [Test]
        public void SpeedAt_StartsAtBaseSpeed()
        {
            var curve = new DifficultyCurve();

            Assert.AreEqual(curve.BaseSpeed, curve.SpeedAt(0f), 0.001f);
        }

        [Test]
        public void SpeedAt_NeverExceedsMaxSpeed()
        {
            var curve = new DifficultyCurve();

            Assert.AreEqual(curve.MaxSpeed, curve.SpeedAt(10_000f), 0.001f);
        }

        [Test]
        public void SpeedAt_IsMonotonicallyIncreasing()
        {
            var curve = new DifficultyCurve();
            float previous = curve.SpeedAt(0f);

            for (float t = 1f; t <= 180f; t += 1f)
            {
                float current = curve.SpeedAt(t);
                Assert.GreaterOrEqual(current, previous, $"{t}초 시점에서 속도가 줄었습니다.");
                previous = current;
            }
        }

        [Test]
        public void IntervalAt_ShrinksOverTimeAndClamps()
        {
            var curve = new DifficultyCurve();

            float start = curve.IntervalAt(0f);
            float mid = curve.IntervalAt(40f);
            float late = curve.IntervalAt(10_000f);

            Assert.Less(mid, start);
            Assert.Less(late, mid);
            Assert.Greater(late, 0f, "생성 간격이 0 이하가 되면 스포너가 폭주한다.");
        }

        [Test]
        public void NormalizedDifficulty_StaysWithinUnitRange()
        {
            var curve = new DifficultyCurve();

            Assert.AreEqual(0f, curve.NormalizedDifficulty(0f), 0.001f);
            Assert.AreEqual(1f, curve.NormalizedDifficulty(10_000f), 0.001f);
        }
    }
}
