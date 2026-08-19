namespace Game.InputSystem
{
    /// <summary>
    /// 게임 로직이 입력 장치를 직접 알지 못하도록 감싸는 인터페이스.
    /// 터치/키보드/자동 플레이(테스트) 구현을 바꿔 끼울 수 있다.
    /// </summary>
    public interface IInputSource
    {
        /// <summary>이번 프레임에 "점프/시작" 입력이 눌렸는가.</summary>
        bool JumpPressed { get; }

        /// <summary>점프 버튼을 계속 누르고 있는가 (가변 점프 높이용).</summary>
        bool JumpHeld { get; }
    }
}
