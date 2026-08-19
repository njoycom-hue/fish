using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 점수/코인 표시와 시작·결과 패널 전환을 담당한다.
    /// <see cref="GameManager.StateChanged"/> 만 구독하므로 게임 로직과 단방향으로 묶인다.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [Header("텍스트")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text coinText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Text resultText;

        [Header("패널")]
        [SerializeField] private GameObject readyPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject pausePanel;

        [Header("버튼")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;

        private GameManager game;

        private void Start()
        {
            game = GameManager.Instance;
            if (game == null)
            {
                Debug.LogError("[HudController] 씬에 GameManager 가 없습니다.");
                enabled = false;
                return;
            }

            game.StateChanged += OnStateChanged;
            game.Score.Changed += OnScoreChanged;

            BindButton(restartButton, () => game.Restart());
            BindButton(homeButton, () => game.ReturnToReady());
            BindButton(pauseButton, () => game.Pause());
            BindButton(resumeButton, () => game.Resume());

            ApplyState(game.State);
            OnScoreChanged(game.Score);
        }

        private void OnDestroy()
        {
            if (game == null)
            {
                return;
            }

            game.StateChanged -= OnStateChanged;
            game.Score.Changed -= OnScoreChanged;
        }

        private void OnStateChanged(GameState previous, GameState current)
        {
            ApplyState(current);
        }

        private void ApplyState(GameState state)
        {
            SetActive(readyPanel, state == GameState.Ready);
            SetActive(gameOverPanel, state == GameState.GameOver);
            SetActive(pausePanel, state == GameState.Paused);

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(state == GameState.Playing);
            }

            if (state == GameState.GameOver)
            {
                ShowResult();
            }

            if (highScoreText != null)
            {
                highScoreText.text = $"BEST {SaveSystem.Data.highScore:N0}";
            }
        }

        private void ShowResult()
        {
            if (resultText == null)
            {
                return;
            }

            string headline = game.IsNewRecord ? "NEW RECORD!" : "GAME OVER";
            resultText.text = $"{headline}\nSCORE {game.Score.Score:N0}\nCOINS {game.Score.Coins:N0}";
        }

        private void OnScoreChanged(ScoreService score)
        {
            if (scoreText != null)
            {
                scoreText.text = score.Score.ToString("N0");
            }

            if (coinText != null)
            {
                coinText.text = $"◎ {score.Coins}";
            }
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null && target.activeSelf != value)
            {
                target.SetActive(value);
            }
        }

        /// <summary>에디터 부트스트랩이 생성한 UI 요소를 주입한다.</summary>
        public void Configure(
            Text score, Text coin, Text highScore, Text result,
            GameObject ready, GameObject gameOver, GameObject pause,
            Button restart, Button home, Button pauseBtn, Button resumeBtn)
        {
            scoreText = score;
            coinText = coin;
            highScoreText = highScore;
            resultText = result;
            readyPanel = ready;
            gameOverPanel = gameOver;
            pausePanel = pause;
            restartButton = restart;
            homeButton = home;
            pauseButton = pauseBtn;
            resumeButton = resumeBtn;
        }
    }
}
