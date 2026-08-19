namespace Game.Core
{
    /// <summary>플레이 세션의 진행 상태.</summary>
    public enum GameState
    {
        /// <summary>시작 대기 — 탭하면 Playing 으로 전환된다.</summary>
        Ready,

        /// <summary>주행 중.</summary>
        Playing,

        /// <summary>일시정지 (Time.timeScale = 0).</summary>
        Paused,

        /// <summary>충돌로 종료됨 — 결과 패널 표시.</summary>
        GameOver
    }
}
