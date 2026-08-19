using System;
using Game.Gameplay;
using Game.InputSystem;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 상태 전이, 주행 속도, 점수를 관리하는 게임의 중심축.
    /// 다른 컴포넌트는 <see cref="RunSpeed"/> 와 <see cref="StateChanged"/> 만 보고 동작한다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("참조")]
        [SerializeField] private PlayerController2D player;
        [SerializeField] private Spawner spawner;

        [Header("난이도")]
        [SerializeField] private DifficultyCurve difficulty = new DifficultyCurve();

        private IInputSource input;
        private float elapsed;

        /// <summary>현재 게임 상태.</summary>
        public GameState State { get; private set; } = GameState.Ready;

        /// <summary>월드가 왼쪽으로 흐르는 속도(units/sec). Playing 이 아니면 0.</summary>
        public float RunSpeed { get; private set; }

        /// <summary>이번 판의 점수 집계.</summary>
        public ScoreService Score { get; } = new ScoreService();

        /// <summary>난이도 곡선 — 스포너가 생성 간격을 물어본다.</summary>
        public DifficultyCurve Difficulty => difficulty;

        /// <summary>이번 판 시작 후 경과 시간(초).</summary>
        public float Elapsed => elapsed;

        /// <summary>이번 판이 최고 기록을 갱신했는지 (GameOver 시점에 확정).</summary>
        public bool IsNewRecord { get; private set; }

        /// <summary>상태가 바뀔 때 발생. (이전 상태, 새 상태)</summary>
        public event Action<GameState, GameState> StateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            input = new TouchInputSource();
            SaveSystem.Load();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            switch (State)
            {
                case GameState.Ready:
                    if (input.JumpPressed)
                    {
                        StartRun();
                    }

                    break;

                case GameState.Playing:
                    Tick(Time.deltaTime);
                    break;
            }
        }

        private void Tick(float deltaTime)
        {
            elapsed += deltaTime;
            RunSpeed = difficulty.SpeedAt(elapsed);
            Score.AddDistance(RunSpeed * deltaTime);
        }

        public void StartRun()
        {
            if (State == GameState.Playing)
            {
                return;
            }

            elapsed = 0f;
            IsNewRecord = false;
            Score.Reset();
            RunSpeed = difficulty.BaseSpeed;

            if (spawner != null)
            {
                spawner.ResetSpawner();
            }

            if (player != null)
            {
                player.ResetPlayer();
            }

            SetState(GameState.Playing);
        }

        public void GameOver()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            RunSpeed = 0f;
            IsNewRecord = SaveSystem.SubmitRun(Score.Score, Score.Coins);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCrash();
            }

            SetState(GameState.GameOver);
        }

        public void Pause()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        /// <summary>결과 화면의 "다시하기" — 씬을 다시 로드하지 않고 상태만 되돌린다.</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            StartRun();
        }

        /// <summary>결과 화면에서 시작 화면으로 돌아간다.</summary>
        public void ReturnToReady()
        {
            Time.timeScale = 1f;
            RunSpeed = 0f;
            elapsed = 0f;
            Score.Reset();

            if (spawner != null)
            {
                spawner.ResetSpawner();
            }

            if (player != null)
            {
                player.ResetPlayer();
            }

            SetState(GameState.Ready);
        }

        private void SetState(GameState next)
        {
            if (State == next)
            {
                return;
            }

            GameState previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }

        private void OnApplicationPause(bool paused)
        {
            // 모바일에서 앱이 백그라운드로 갈 때 판이 그냥 진행되면 억울한 죽음이 생긴다.
            if (paused && State == GameState.Playing)
            {
                Pause();
            }
        }
    }
}
