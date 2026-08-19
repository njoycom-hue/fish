using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 한 판의 점수를 계산한다. 이동 거리 1 unit 당 1점, 코인 1개당 <see cref="CoinValue"/> 점.
    /// UnityEngine 오브젝트에 의존하지 않으므로 단위 테스트가 쉽다.
    /// </summary>
    public class ScoreService
    {
        public const int CoinValue = 10;

        private float distance;

        /// <summary>이번 판에 달린 거리(unit).</summary>
        public float Distance => distance;

        /// <summary>이번 판에 먹은 코인 개수.</summary>
        public int Coins { get; private set; }

        /// <summary>거리 + 코인을 합산한 최종 점수.</summary>
        public int Score => Mathf.FloorToInt(distance) + (Coins * CoinValue);

        /// <summary>점수가 바뀔 때마다 발생 — HUD 갱신용.</summary>
        public event Action<ScoreService> Changed;

        public void AddDistance(float delta)
        {
            if (delta <= 0f)
            {
                return;
            }

            distance += delta;
            Changed?.Invoke(this);
        }

        public void AddCoins(int amount = 1)
        {
            if (amount <= 0)
            {
                return;
            }

            Coins += amount;
            Changed?.Invoke(this);
        }

        public void Reset()
        {
            distance = 0f;
            Coins = 0;
            Changed?.Invoke(this);
        }
    }
}
