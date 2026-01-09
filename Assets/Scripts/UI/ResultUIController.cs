using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sobada.Managers;

namespace Sobada.UI
{
    /// <summary>
    /// リザルト画面のUI管理
    /// </summary>
    public class ResultUIController : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button titleButton;

        [Header("スコア表示")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI scoreRankText;

        [Header("統計表示")]
        [SerializeField] private TextMeshProUGUI clearedWordsText;
        [SerializeField] private TextMeshProUGUI typedCharactersText;
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private TextMeshProUGUI typingSpeedText;
        [SerializeField] private TextMeshProUGUI maxComboText;
        [SerializeField] private TextMeshProUGUI missTypesText;

        [Header("評価設定")]
        [SerializeField] private int sRankScore = 10000;
        [SerializeField] private int aRankScore = 7000;
        [SerializeField] private int bRankScore = 5000;
        [SerializeField] private int cRankScore = 3000;

        [Header("アニメーション")]
        [SerializeField] private float scoreCountUpDuration = 2f;

        private int displayScore = 0;
        private int targetScore = 0;

        private void Start()
        {
            // ボタンのイベント設定
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryButtonClicked);
            }

            if (titleButton != null)
            {
                titleButton.onClick.AddListener(OnTitleButtonClicked);
            }

            // GameManagerのイベントに登録
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged.AddListener(OnGameStateChanged);
                GameManager.Instance.OnGameOver.AddListener(OnGameOver);
            }

            // 初期状態では非表示
            gameObject.SetActive(false);
        }

        private void Update()
        {
            // スコアのカウントアップアニメーション
            if (displayScore < targetScore)
            {
                int step = Mathf.CeilToInt((targetScore - displayScore) * Time.deltaTime / scoreCountUpDuration);
                displayScore = Mathf.Min(displayScore + Mathf.Max(1, step), targetScore);
                UpdateScoreDisplay();
            }
        }

        private void OnGameOver()
        {
            if (GameManager.Instance == null)
                return;

            // 最終スコアを設定
            targetScore = GameManager.Instance.CurrentScore;
            displayScore = 0;

            // 統計情報を表示
            DisplayStatistics();

            // ランクを表示
            DisplayRank();
        }

        private void DisplayStatistics()
        {
            if (GameManager.Instance == null)
                return;

            if (clearedWordsText != null)
            {
                clearedWordsText.text = $"クリアした単語: {GameManager.Instance.TotalClearedWords}";
            }

            if (typedCharactersText != null)
            {
                typedCharactersText.text = $"タイプした文字数: {GameManager.Instance.TotalTypedCharacters}";
            }

            if (accuracyText != null)
            {
                float accuracy = GameManager.Instance.GetAccuracy();
                accuracyText.text = $"正確性: {accuracy:F1}%";
            }

            if (typingSpeedText != null)
            {
                float speed = GameManager.Instance.GetTypingSpeed();
                typingSpeedText.text = $"タイピング速度: {speed:F1} 文字/秒";
            }

            if (maxComboText != null)
            {
                maxComboText.text = $"最大コンボ: {GameManager.Instance.MaxCombo}";
            }

            if (missTypesText != null)
            {
                missTypesText.text = $"ミスタイプ: {GameManager.Instance.TotalMissTypes}";
            }
        }

        private void DisplayRank()
        {
            if (scoreRankText == null || GameManager.Instance == null)
                return;

            int score = GameManager.Instance.CurrentScore;
            string rank;
            Color rankColor;

            if (score >= sRankScore)
            {
                rank = "S";
                rankColor = new Color(1f, 0.84f, 0f); // ゴールド
            }
            else if (score >= aRankScore)
            {
                rank = "A";
                rankColor = new Color(0.75f, 0.75f, 0.75f); // シルバー
            }
            else if (score >= bRankScore)
            {
                rank = "B";
                rankColor = new Color(0.8f, 0.5f, 0.2f); // ブロンズ
            }
            else if (score >= cRankScore)
            {
                rank = "C";
                rankColor = Color.green;
            }
            else
            {
                rank = "D";
                rankColor = Color.gray;
            }

            scoreRankText.text = $"ランク: {rank}";
            scoreRankText.color = rankColor;
        }

        private void UpdateScoreDisplay()
        {
            if (finalScoreText != null)
            {
                finalScoreText.text = $"¥{displayScore:N0}";
            }
        }

        private void OnRetryButtonClicked()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentConfig != null)
            {
                // 同じ設定でリトライ
                GameManager.Instance.StartGame(GameManager.Instance.CurrentConfig);
            }
        }

        private void OnTitleButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToTitle();
            }
        }

        private void OnGameStateChanged(GameState newState)
        {
            gameObject.SetActive(newState == GameState.Result);

            if (newState == GameState.Result)
            {
                // リザルト画面表示時にアニメーション開始
                displayScore = 0;
            }
        }

        private void OnDestroy()
        {
            // イベントのクリーンアップ
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged.RemoveListener(OnGameStateChanged);
                GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
            }
        }
    }
}
