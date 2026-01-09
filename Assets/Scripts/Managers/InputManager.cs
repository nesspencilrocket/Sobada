using UnityEngine;
using UnityEngine.Events;
using System.Text;

namespace Sobada.Managers
{
    /// <summary>
    /// キーボード入力を管理するクラス
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("入力設定")]
        [SerializeField] private bool enableInput = true;

        [Header("現在の入力状態")]
        [SerializeField] private string targetText = "";
        [SerializeField] private StringBuilder currentInput = new StringBuilder();
        [SerializeField] private int currentPosition = 0;

        // イベント
        public UnityEvent<char> OnCorrectInput = new UnityEvent<char>();
        public UnityEvent<char> OnIncorrectInput = new UnityEvent<char>();
        public UnityEvent<string> OnWordCompleted = new UnityEvent<string>();
        public UnityEvent<string> OnInputChanged = new UnityEvent<string>();

        // プロパティ
        public string TargetText => targetText;
        public string CurrentInput => currentInput.ToString();
        public int CurrentPosition => currentPosition;
        public bool IsInputEnabled => enableInput;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (!enableInput || GameManager.Instance == null)
                return;

            if (GameManager.Instance.CurrentState != GameState.Playing)
                return;

            ProcessInput();
        }

        /// <summary>
        /// 新しいターゲット単語を設定
        /// </summary>
        public void SetTargetWord(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                Debug.LogWarning("InputManager: 空の単語が設定されました");
                return;
            }

            targetText = word.ToLower();
            currentInput.Clear();
            currentPosition = 0;

            OnInputChanged?.Invoke(currentInput.ToString());

            Debug.Log($"ターゲット単語設定: {targetText}");
        }

        /// <summary>
        /// 入力をクリア
        /// </summary>
        public void ClearInput()
        {
            targetText = "";
            currentInput.Clear();
            currentPosition = 0;

            OnInputChanged?.Invoke(currentInput.ToString());
        }

        /// <summary>
        /// 入力の有効/無効を切り替え
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            enableInput = enabled;
        }

        /// <summary>
        /// 次にタイプすべき文字を取得
        /// </summary>
        public char GetNextCharacter()
        {
            if (string.IsNullOrEmpty(targetText) || currentPosition >= targetText.Length)
                return '\0';

            return targetText[currentPosition];
        }

        /// <summary>
        /// 入力の進捗率を取得（0.0～1.0）
        /// </summary>
        public float GetProgress()
        {
            if (string.IsNullOrEmpty(targetText))
                return 0f;

            return (float)currentPosition / targetText.Length;
        }

        /// <summary>
        /// 残りの文字数を取得
        /// </summary>
        public int GetRemainingCharacters()
        {
            if (string.IsNullOrEmpty(targetText))
                return 0;

            return targetText.Length - currentPosition;
        }

        private void ProcessInput()
        {
            if (string.IsNullOrEmpty(targetText))
                return;

            // 文字キーの入力を検出
            foreach (char c in Input.inputString)
            {
                // 制御文字は無視
                if (char.IsControl(c))
                    continue;

                ProcessCharacter(c);
            }

            // バックスペースの処理
            if (Input.GetKeyDown(KeyCode.Backspace) && currentPosition > 0)
            {
                currentPosition--;
                currentInput.Remove(currentPosition, 1);
                OnInputChanged?.Invoke(currentInput.ToString());
            }
        }

        private void ProcessCharacter(char inputChar)
        {
            char lowerChar = char.ToLower(inputChar);
            char expectedChar = GetNextCharacter();

            if (expectedChar == '\0')
                return;

            if (lowerChar == expectedChar)
            {
                // 正解
                currentInput.Append(lowerChar);
                currentPosition++;

                OnCorrectInput?.Invoke(lowerChar);
                OnInputChanged?.Invoke(currentInput.ToString());

                // GameManagerに通知
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnCharacterTyped();
                }

                // 単語完成チェック
                if (currentPosition >= targetText.Length)
                {
                    OnWordCompleted?.Invoke(targetText);
                    Debug.Log($"単語完成: {targetText}");
                }
            }
            else
            {
                // ミス
                OnIncorrectInput?.Invoke(lowerChar);

                // GameManagerに通知
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnMissType();
                }

                Debug.Log($"ミスタイプ: 入力={lowerChar}, 期待={expectedChar}");
            }
        }

        /// <summary>
        /// デバッグ情報を取得
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Target: {targetText}\n" +
                   $"Current: {currentInput}\n" +
                   $"Position: {currentPosition}/{targetText.Length}\n" +
                   $"Next: {GetNextCharacter()}\n" +
                   $"Progress: {GetProgress() * 100:F1}%";
        }
    }
}
