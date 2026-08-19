using UnityEngine;

namespace Game.InputSystem
{
    /// <summary>
    /// 모바일 터치 + 에디터용 마우스/스페이스바를 함께 처리하는 기본 입력 구현.
    /// 레거시 Input Manager 만 사용하므로 추가 패키지 설정 없이 동작한다.
    /// </summary>
    public class TouchInputSource : IInputSource
    {
        public bool JumpPressed
        {
            get
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    return true;
                }

                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.GetTouch(i).phase == TouchPhase.Began)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool JumpHeld
        {
            get
            {
                if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
                {
                    return true;
                }

                for (int i = 0; i < Input.touchCount; i++)
                {
                    TouchPhase phase = Input.GetTouch(i).phase;
                    if (phase == TouchPhase.Began || phase == TouchPhase.Moved || phase == TouchPhase.Stationary)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
