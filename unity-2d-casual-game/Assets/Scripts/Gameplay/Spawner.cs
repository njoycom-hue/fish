using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// 난이도 곡선에 맞춰 장애물과 코인을 생성한다.
    /// 생성/파괴 대신 <see cref="ObjectPool{T}"/> 로 재사용해 모바일에서 GC 스파이크를 피한다.
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        [Header("프리팹")]
        [SerializeField] private Scroller obstaclePrefab;
        [SerializeField] private Scroller coinPrefab;

        [Header("생성 위치")]
        [SerializeField] private float spawnX = 12f;
        [SerializeField] private float groundY = -3f;

        [Tooltip("장애물 중심이 바닥면보다 얼마나 위에 놓이는가 (프리팹 높이의 절반)")]
        [SerializeField] private float obstacleYOffset = 0.8f;

        [Tooltip("코인이 뜨는 높이 범위 (바닥 기준)")]
        [SerializeField] private Vector2 coinHeightRange = new Vector2(1.2f, 3.4f);

        [Header("확률")]
        [Tooltip("장애물 하나를 만들 때 코인 줄을 함께 놓을 확률")]
        [SerializeField, Range(0f, 1f)] private float coinLineChance = 0.65f;
        [SerializeField] private int coinsPerLine = 3;
        [SerializeField] private float coinSpacing = 1.1f;

        [Tooltip("장애물을 두 개 붙여 놓기 시작하는 난이도(0~1)")]
        [SerializeField, Range(0f, 1f)] private float doubleObstacleThreshold = 0.55f;

        private ObjectPool<Scroller> obstaclePool;
        private ObjectPool<Scroller> coinPool;
        private float nextSpawnTime;

        private void Awake()
        {
            if (obstaclePrefab != null)
            {
                obstaclePool = new ObjectPool<Scroller>(obstaclePrefab, transform, prewarm: 6);
            }

            if (coinPrefab != null)
            {
                coinPool = new ObjectPool<Scroller>(coinPrefab, transform, prewarm: 12);
            }
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.State != GameState.Playing)
            {
                return;
            }

            if (Time.time < nextSpawnTime)
            {
                return;
            }

            SpawnWave(game);
            nextSpawnTime = Time.time + game.Difficulty.IntervalAt(game.Elapsed);
        }

        private void SpawnWave(GameManager game)
        {
            float difficulty = game.Difficulty.NormalizedDifficulty(game.Elapsed);

            SpawnObstacle(spawnX);

            // 후반부에는 장애물을 가끔 두 개씩 붙여 점프 타이밍을 더 빡빡하게 만든다.
            if (difficulty >= doubleObstacleThreshold && Random.value < difficulty * 0.5f)
            {
                SpawnObstacle(spawnX + Random.Range(1.6f, 2.4f));
            }

            if (coinPool != null && Random.value < coinLineChance)
            {
                SpawnCoinLine();
            }
        }

        private void SpawnObstacle(float x)
        {
            if (obstaclePool == null)
            {
                return;
            }

            Scroller instance = obstaclePool.Get(
                new Vector3(x, groundY + obstacleYOffset, 0f), Quaternion.identity);
            instance.Despawned = ReleaseObstacle;
        }

        private void SpawnCoinLine()
        {
            float height = Random.Range(coinHeightRange.x, coinHeightRange.y);
            float startX = spawnX + Random.Range(2.5f, 4.5f);

            for (int i = 0; i < coinsPerLine; i++)
            {
                Vector3 position = new Vector3(startX + (i * coinSpacing), groundY + height, 0f);
                Scroller instance = coinPool.Get(position, Quaternion.identity);
                instance.Despawned = ReleaseCoin;
            }
        }

        private void ReleaseObstacle(Scroller instance) => obstaclePool?.Release(instance);

        private void ReleaseCoin(Scroller instance) => coinPool?.Release(instance);

        /// <summary>새 판을 시작할 때 화면 위 오브젝트를 모두 걷어낸다.</summary>
        public void ResetSpawner()
        {
            obstaclePool?.ReleaseAll();
            coinPool?.ReleaseAll();

            // 시작 직후 바로 장애물이 튀어나오면 반응할 시간이 없다.
            nextSpawnTime = Time.time + 1.2f;
        }

        /// <summary>에디터 부트스트랩이 프리팹을 주입할 때 사용한다.</summary>
        public void Configure(Scroller obstacle, Scroller coin, float groundLevel)
        {
            obstaclePrefab = obstacle;
            coinPrefab = coin;
            groundY = groundLevel;
        }
    }
}
