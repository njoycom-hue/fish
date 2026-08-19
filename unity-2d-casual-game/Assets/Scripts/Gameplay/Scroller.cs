using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// 오브젝트를 <see cref="GameManager.RunSpeed"/> 로 왼쪽으로 흘려보낸다.
    /// 플레이어는 제자리에 있고 월드가 움직이는 구조라 카메라 처리가 단순해진다.
    /// </summary>
    public class Scroller : MonoBehaviour
    {
        [Tooltip("1보다 크면 더 빠르게 — 배경 패럴랙스에 쓴다")]
        [SerializeField] private float speedMultiplier = 1f;

        [Tooltip("이 x 좌표보다 왼쪽으로 나가면 사라진다")]
        [SerializeField] private float despawnX = -14f;

        /// <summary>화면 밖으로 나갔을 때 호출할 콜백 — 스포너가 풀 반환을 연결한다.</summary>
        public System.Action<Scroller> Despawned;

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.State != GameState.Playing)
            {
                return;
            }

            transform.Translate(Vector3.left * (game.RunSpeed * speedMultiplier * Time.deltaTime));

            if (transform.position.x < despawnX)
            {
                Despawn();
            }
        }

        /// <summary>즉시 회수를 요청한다 — 코인을 먹었을 때처럼 화면 안에서 사라질 때 쓴다.</summary>
        public void Despawn()
        {
            Despawned?.Invoke(this);
        }
    }
}
