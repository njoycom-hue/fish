using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class ScoreServiceTests
    {
        [Test]
        public void Score_CountsDistanceAsWholeUnits()
        {
            var score = new ScoreService();

            score.AddDistance(12.9f);

            Assert.AreEqual(12, score.Score);
        }

        [Test]
        public void Score_AddsCoinValuePerCoin()
        {
            var score = new ScoreService();

            score.AddDistance(10f);
            score.AddCoins(3);

            Assert.AreEqual(10 + (3 * ScoreService.CoinValue), score.Score);
            Assert.AreEqual(3, score.Coins);
        }

        [Test]
        public void Score_IgnoresNonPositiveInput()
        {
            var score = new ScoreService();

            score.AddDistance(-5f);
            score.AddCoins(0);
            score.AddCoins(-2);

            Assert.AreEqual(0, score.Score);
            Assert.AreEqual(0, score.Coins);
        }

        [Test]
        public void Reset_ClearsDistanceAndCoins()
        {
            var score = new ScoreService();
            score.AddDistance(50f);
            score.AddCoins(4);

            score.Reset();

            Assert.AreEqual(0, score.Score);
            Assert.AreEqual(0f, score.Distance);
            Assert.AreEqual(0, score.Coins);
        }

        [Test]
        public void Changed_FiresOnEveryScoringEvent()
        {
            var score = new ScoreService();
            int notifications = 0;
            score.Changed += _ => notifications++;

            score.AddDistance(1f);
            score.AddCoins();
            score.AddDistance(-1f); // 무시되므로 알림도 없어야 한다
            score.Reset();

            Assert.AreEqual(3, notifications);
        }
    }
}
