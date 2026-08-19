using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 경과 시간에 따라 주행 속도와 장애물 간격을 결정한다.
    /// MonoBehaviour 가 아닌 순수 클래스라 EditMode 테스트에서 바로 검증할 수 있다.
    /// </summary>
    [Serializable]
    public class DifficultyCurve
    {
        [Header("주행 속도 (units/sec)")]
        [SerializeField] private float baseSpeed = 5f;
        [SerializeField] private float maxSpeed = 14f;

        [Tooltip("최고 속도에 도달하기까지 걸리는 시간(초)")]
        [SerializeField] private float secondsToMaxSpeed = 90f;

        [Header("장애물 생성 간격 (초)")]
        [SerializeField] private float baseInterval = 1.6f;
        [SerializeField] private float minInterval = 0.62f;

        [Tooltip("간격이 최소값까지 줄어드는 데 걸리는 시간(초)")]
        [SerializeField] private float secondsToMinInterval = 75f;

        public float BaseSpeed => baseSpeed;
        public float MaxSpeed => maxSpeed;

        /// <summary>경과 시간 <paramref name="elapsed"/> 초 시점의 주행 속도.</summary>
        public float SpeedAt(float elapsed)
        {
            float t = Progress(elapsed, secondsToMaxSpeed);
            return Mathf.Lerp(baseSpeed, maxSpeed, t);
        }

        /// <summary>경과 시간 <paramref name="elapsed"/> 초 시점의 장애물 생성 간격.</summary>
        public float IntervalAt(float elapsed)
        {
            float t = Progress(elapsed, secondsToMinInterval);
            return Mathf.Lerp(baseInterval, minInterval, t);
        }

        /// <summary>0(시작) ~ 1(최대 난이도) 로 정규화된 난이도 진행도.</summary>
        public float NormalizedDifficulty(float elapsed)
        {
            return Progress(elapsed, Mathf.Max(secondsToMaxSpeed, secondsToMinInterval));
        }

        private static float Progress(float elapsed, float duration)
        {
            if (duration <= 0f)
            {
                return 1f;
            }

            // SmoothStep 을 써서 초반 난이도 상승을 완만하게 만든다.
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
        }
    }
}
