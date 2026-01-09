using UnityEngine;
using UnityEngine.Events;
using Sobada.Data;

namespace Sobada.Managers
{
    /// <summary>
    /// ゲーム全体の状態を管理するシングルトン
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("ゲーム設定")]
        [SerializeField] private GameConfigData currentConfig;

        [Header("ゲーム状態")]
        [SerializeField] private GameState currentState = GameState.Title;
        [SerializeField] private int currentScore = 0;
        [SerializeField] private float remainingTime = 0f;
        [SerializeField] private int currentPhaseIndex = 0;

        [Header("統計情報")]
        [SerializeField] private int totalTypedCharacters = 0;
        [SerializeField] private int totalMissTypes = 0;
        [SerializeField] private int totalClearedWords = 0;
        [SerializeField] private int currentCombo = 0;
        [SerializeField] private int maxCombo = 0;

        // イベント
        public UnityEvent<int> OnScoreChanged = new UnityEvent<int>();
        public UnityEvent<float> OnTimeChanged = new UnityEvent<float>();
        public UnityEvent<int> OnPhaseChanged = new UnityEvent<int>();
        public UnityEvent<int> OnComboChanged = new UnityEvent<int>();
        public UnityEvent<GameState> OnGameStateChanged = new UnityEvent<GameState>();
        public UnityEvent OnGameOver = new UnityEvent();

        // プロパティ
        public GameConfigData CurrentConfig => currentConfig;
        public GameState CurrentState => currentState;
        public int CurrentScore => currentScore;
        public float RemainingTime => remainingTime;
        public int CurrentPhaseIndex => currentPhaseIndex;
        public int TotalTypedCharacters => totalTypedCharacters;
        public int TotalMissTypes => totalMissTypes;
        public int TotalClearedWords => totalClearedWords;
        public int CurrentCombo => currentCombo;
        public int MaxCombo => maxCombo;

        private float gameStartTime;
        private float phaseStartTime;

        private void Awake()
        {
            // シングルトンの設定
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                UpdateGameTime();
            }
        }

        /// <summary>
        /// ゲームを開始
        /// </summary>
        public void StartGame(GameConfigData config)
        {
            if (config == null || !config.IsValid())
            {
                Debug.LogError("GameManager: 無効なGameConfigDataです");
                return;
            }

            currentConfig = config;
            ResetGameStats();

            remainingTime = config.totalGameTime;
            gameStartTime = Time.time;
            phaseStartTime = Time.time;

            ChangeState(GameState.Playing);
            ChangePhase(0);

            Debug.Log($"ゲーム開始: {config.configName}");
        }

        /// <summary>
        /// ゲームを終了
        /// </summary>
        public void EndGame()
        {
            ChangeState(GameState.Result);
            OnGameOver?.Invoke();

            Debug.Log($"ゲーム終了 - 最終スコア: {currentScore}円");
        }

        /// <summary>
        /// ゲームを一時停止
        /// </summary>
        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
                Time.timeScale = 0f;
            }
        }

        /// <summary>
        /// ゲームを再開
        /// </summary>
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
                Time.timeScale = 1f;
            }
        }

        /// <summary>
        /// タイトルに戻る
        /// </summary>
        public void ReturnToTitle()
        {
            ChangeState(GameState.Title);
            ResetGameStats();
            Time.timeScale = 1f;
        }

        /// <summary>
        /// スコアを加算
        /// </summary>
        public void AddScore(int points)
        {
            if (currentState != GameState.Playing)
                return;

            // コンボ倍率を適用
            float multiplier = currentConfig.GetComboMultiplier(currentCombo);
            int finalPoints = Mathf.RoundToInt(points * multiplier);

            currentScore += finalPoints;
            OnScoreChanged?.Invoke(currentScore);

            // フェーズ移行チェック（スコアベース）
            CheckPhaseTransition();

            Debug.Log($"スコア加算: +{finalPoints}円 (基本: {points}円, 倍率: x{multiplier:F2})");
        }

        /// <summary>
        /// 単語クリア時の処理
        /// </summary>
        public void OnWordCleared(WordData word)
        {
            if (currentState != GameState.Playing)
                return;

            totalClearedWords++;
            currentCombo++;

            if (currentCombo > maxCombo)
            {
                maxCombo = currentCombo;
            }

            OnComboChanged?.Invoke(currentCombo);
            AddScore(word.scoreValue);
        }

        /// <summary>
        /// 文字タイプ時の処理
        /// </summary>
        public void OnCharacterTyped()
        {
            if (currentState != GameState.Playing)
                return;

            totalTypedCharacters++;
        }

        /// <summary>
        /// ミスタイプ時の処理
        /// </summary>
        public void OnMissType()
        {
            if (currentState != GameState.Playing)
                return;

            totalMissTypes++;
            currentCombo = 0;
            OnComboChanged?.Invoke(currentCombo);

            // ペナルティを適用
            if (currentConfig.missTypePenalty > 0)
            {
                currentScore = Mathf.Max(0, currentScore - currentConfig.missTypePenalty);
                OnScoreChanged?.Invoke(currentScore);
            }
        }

        /// <summary>
        /// 正確性を計算
        /// </summary>
        public float GetAccuracy()
        {
            int totalTypes = totalTypedCharacters + totalMissTypes;
            if (totalTypes == 0)
                return 100f;

            return (float)totalTypedCharacters / totalTypes * 100f;
        }

        /// <summary>
        /// 1秒あたりのタイプ数を計算
        /// </summary>
        public float GetTypingSpeed()
        {
            float elapsedTime = Time.time - gameStartTime;
            if (elapsedTime <= 0)
                return 0f;

            return totalTypedCharacters / elapsedTime;
        }

        private void UpdateGameTime()
        {
            remainingTime -= Time.deltaTime;
            OnTimeChanged?.Invoke(remainingTime);

            if (remainingTime <= 0)
            {
                remainingTime = 0;
                EndGame();
            }
            else
            {
                // フェーズ移行チェック（時間ベース）
                CheckPhaseTransitionByTime();
            }
        }

        private void CheckPhaseTransition()
        {
            if (currentConfig == null || currentConfig.phases == null)
                return;

            int newPhaseIndex = currentConfig.GetCurrentPhaseIndex(currentScore);
            if (newPhaseIndex != currentPhaseIndex && newPhaseIndex < currentConfig.GetPhaseCount())
            {
                ChangePhase(newPhaseIndex);
            }
        }

        private void CheckPhaseTransitionByTime()
        {
            if (currentConfig == null || currentConfig.phases == null)
                return;

            float elapsedTime = Time.time - gameStartTime;
            int newPhaseIndex = currentConfig.GetCurrentPhaseIndexByTime(elapsedTime);
            
            if (newPhaseIndex != currentPhaseIndex)
            {
                ChangePhase(newPhaseIndex);
            }
        }

        private void ChangePhase(int newPhaseIndex)
        {
            if (currentConfig == null || newPhaseIndex >= currentConfig.GetPhaseCount())
                return;

            currentPhaseIndex = newPhaseIndex;
            phaseStartTime = Time.time;
            OnPhaseChanged?.Invoke(currentPhaseIndex);

            PhaseConfig phase = currentConfig.GetPhaseByIndex(currentPhaseIndex);
            Debug.Log($"フェーズ変更: {phase.phaseName}");
        }

        private void ChangeState(GameState newState)
        {
            if (currentState == newState)
                return;

            currentState = newState;
            OnGameStateChanged?.Invoke(currentState);

            Debug.Log($"ゲーム状態変更: {currentState}");
        }

        private void ResetGameStats()
        {
            currentScore = 0;
            totalTypedCharacters = 0;
            totalMissTypes = 0;
            totalClearedWords = 0;
            currentCombo = 0;
            maxCombo = 0;
            currentPhaseIndex = 0;

            OnScoreChanged?.Invoke(currentScore);
            OnComboChanged?.Invoke(currentCombo);
        }

        /// <summary>
        /// 現在のフェーズ設定を取得
        /// </summary>
        public PhaseConfig GetCurrentPhase()
        {
            return currentConfig?.GetPhaseByIndex(currentPhaseIndex);
        }
    }

    /// <summary>
    /// ゲームの状態
    /// </summary>
    public enum GameState
    {
        Title,      // タイトル画面
        Playing,    // プレイ中
        Paused,     // 一時停止
        Result      // リザルト画面
    }
}
