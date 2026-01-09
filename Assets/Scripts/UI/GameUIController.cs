using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sobada.Managers;

namespace Sobada.UI
{
    /// <summary>
    /// ゲーム中のUI表示を管理
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        [Header("スコア表示")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI scoreLabel;

        [Header("時間表示")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Image timeBar;

        [Header("コンボ表示")]
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private GameObject comboPanel;
        [SerializeField] private int minComboToShow = 3;

        [Header("フェーズ表示")]
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private GameObject phaseTransitionPanel;
        [SerializeField] private TextMeshProUGUI phaseTransitionText;

        [Header("現在の入力表示")]
        [SerializeField] private TextMeshProUGUI currentInputText;
        [SerializeField] private TextMeshProUGUI targetWordText;

        [Header("統計表示")]
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private TextMeshProUGUI typingSpeedText;

        [Header("アニメーション設定")]
        [SerializeField] private float scoreAnimationSpeed = 100f;
        [SerializeField] private float comboScaleAnimation = 1.2f;

        private int displayedScore = 0;
        private int targetScore = 0;
        private float maxTime = 60f;

        private void Start()
        {
            // GameManagerのイベントに登録
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged.AddListener(OnScoreChanged);
                GameManager.Instance.OnTimeChanged.AddListener(OnTimeChanged);
                GameManager.Instance.OnComboChanged.AddListener(OnComboChanged);
                GameManager.Instance.OnPhaseChanged.AddListener(OnPhaseChanged);
                GameManager.Instance.OnGameStateChanged.AddListener(OnGameStateChanged);

                // 初期値設定
                if (GameManager.Instance.CurrentConfig != null)
                {
                    maxTime = GameManager.Instance.CurrentConfig.totalGameTime;
                }
            }

            // InputManagerのイベントに登録
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnInputChanged.AddListener(OnInputChanged);
            }

            // 初期表示
            UpdateScoreDisplay(0);
            UpdateTimeDisplay(maxTime);
            UpdateComboDisplay(0);

            if (comboPanel != null)
            {
                comboPanel.SetActive(false);
            }

            if (phaseTransitionPanel != null)
            {
                phaseTransitionPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // スコアのアニメーション
            if (displayedScore != targetScore)
            {
                AnimateScore();
            }

            // 統計情報の更新
            UpdateStatistics();
        }

        private void OnScoreChanged(int newScore)
        {
            targetScore = newScore;
        }

        private void OnTimeChanged(float remainingTime)
        {
            UpdateTimeDisplay(remainingTime);
        }

        private void OnComboChanged(int combo)
        {
            UpdateComboDisplay(combo);
        }

        private void OnPhaseChanged(int phaseIndex)
        {
            if (GameManager.Instance == null)
                return;

            var phase = GameManager.Instance.GetCurrentPhase();
            if (phase != null)
            {
                ShowPhaseTransition(phase.phaseName);
                UpdatePhaseDisplay(phase.phaseName);
            }
        }

        private void OnGameStateChanged(GameState newState)
        {
            gameObject.SetActive(newState == GameState.Playing);
        }

        private void OnInputChanged(string input)
        {
            if (currentInputText != null)
            {
                currentInputText.text = input;
            }

            if (targetWordText != null && InputManager.Instance != null)
            {
                targetWordText.text = InputManager.Instance.TargetText;
            }
        }

        private void AnimateScore()
        {
            int difference = targetScore - displayedScore;
            int step = Mathf.CeilToInt(scoreAnimationSpeed * Time.deltaTime);

            if (Mathf.Abs(difference) <= step)
            {
                displayedScore = targetScore;
            }
            else
            {
                displayedScore += (difference > 0 ? step : -step);
            }

            UpdateScoreDisplay(displayedScore);
        }

        private void UpdateScoreDisplay(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"¥{score:N0}";
            }
        }

        private void UpdateTimeDisplay(float time)
        {
            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                timeText.text = $"{minutes:00}:{seconds:00}";

                // 残り時間が少ない場合は赤色に
                if (time <= 10f)
                {
                    timeText.color = Color.red;
                }
                else if (time <= 30f)
                {
                    timeText.color = Color.yellow;
                }
                else
                {
                    timeText.color = Color.white;
                }
            }

            if (timeBar != null)
            {
                timeBar.fillAmount = time / maxTime;
            }
        }

        private void UpdateComboDisplay(int combo)
        {
            if (comboPanel != null)
            {
                comboPanel.SetActive(combo >= minComboToShow);
            }

            if (comboText != null)
            {
                comboText.text = $"{combo} COMBO!";

                // コンボ数に応じて色を変更
                if (combo >= 20)
                {
                    comboText.color = Color.magenta;
                }
                else if (combo >= 10)
                {
                    comboText.color = Color.red;
                }
                else if (combo >= 5)
                {
                    comboText.color = Color.yellow;
                }
                else
                {
                    comboText.color = Color.white;
                }

                // スケールアニメーション
                if (combo > 0)
                {
                    StartCoroutine(ComboScaleAnimation());
                }
            }
        }

        private void UpdatePhaseDisplay(string phaseName)
        {
            if (phaseText != null)
            {
                phaseText.text = phaseName;
            }
        }

        private void ShowPhaseTransition(string phaseName)
        {
            if (phaseTransitionPanel != null && phaseTransitionText != null)
            {
                phaseTransitionText.text = phaseName;
                StartCoroutine(PhaseTransitionAnimation());
            }
        }

        private void UpdateStatistics()
        {
            if (GameManager.Instance == null)
                return;

            if (accuracyText != null)
            {
                float accuracy = GameManager.Instance.GetAccuracy();
                accuracyText.text = $"正確性: {accuracy:F1}%";
            }

            if (typingSpeedText != null)
            {
                float speed = GameManager.Instance.GetTypingSpeed();
                typingSpeedText.text = $"速度: {speed:F1} 文字/秒";
            }
        }

        private System.Collections.IEnumerator ComboScaleAnimation()
        {
            if (comboText == null)
                yield break;

            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = originalScale * comboScaleAnimation;

            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                comboText.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            comboText.transform.localScale = originalScale;
        }

        private System.Collections.IEnumerator PhaseTransitionAnimation()
        {
            if (phaseTransitionPanel == null)
                yield break;

            CanvasGroup canvasGroup = phaseTransitionPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = phaseTransitionPanel.AddComponent<CanvasGroup>();
            }

            phaseTransitionPanel.SetActive(true);

            // フェードイン
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = elapsed / duration;
                yield return null;
            }

            canvasGroup.alpha = 1f;

            // 表示時間
            yield return new WaitForSeconds(1.5f);

            // フェードアウト
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            phaseTransitionPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            // イベントのクリーンアップ
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged.RemoveListener(OnScoreChanged);
                GameManager.Instance.OnTimeChanged.RemoveListener(OnTimeChanged);
                GameManager.Instance.OnComboChanged.RemoveListener(OnComboChanged);
                GameManager.Instance.OnPhaseChanged.RemoveListener(OnPhaseChanged);
                GameManager.Instance.OnGameStateChanged.RemoveListener(OnGameStateChanged);
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnInputChanged.RemoveListener(OnInputChanged);
            }
        }
    }
}
