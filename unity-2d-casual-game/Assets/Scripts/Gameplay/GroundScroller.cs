using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// 바닥 타일 여러 개를 왼쪽으로 흘리다가, 화면 밖으로 나간 타일을
    /// 가장 오른쪽 타일 뒤에 붙여 무한 바닥을 만든다.
    /// </summary>
    public class GroundScroller : MonoBehaviour
    {
        [SerializeField] private Transform[] tiles;
        [SerializeField] private float tileWidth = 20f;
        [SerializeField] private float recycleX = -20f;

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.State != GameState.Playing || tiles == null)
            {
                return;
            }

            float step = game.RunSpeed * Time.deltaTime;

            foreach (Transform tile in tiles)
            {
                if (tile == null)
                {
                    continue;
                }

                tile.Translate(Vector3.left * step);

                if (tile.position.x <= recycleX)
                {
                    tile.position = new Vector3(
                        RightmostX() + tileWidth,
                        tile.position.y,
                        tile.position.z);
                }
            }
        }

        private float RightmostX()
        {
            float max = float.NegativeInfinity;
            foreach (Transform tile in tiles)
            {
                if (tile != null && tile.position.x > max)
                {
                    max = tile.position.x;
                }
            }

            return max;
        }

        /// <summary>에디터 부트스트랩이 타일을 만들어 준 뒤 주입한다.</summary>
        public void Configure(Transform[] groundTiles, float width, float recycleThreshold)
        {
            tiles = groundTiles;
            tileWidth = width;
            recycleX = recycleThreshold;
        }
    }
}
