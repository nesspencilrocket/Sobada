using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sobada.Data;
using Sobada.Managers;

namespace Sobada.GameObjects
{
    /// <summary>
    /// 蕎麦オブジェクトの動作を制御
    /// </summary>
    public class SobaController : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private TextMeshProUGUI romajiText;
        [SerializeField] private TextMeshProUGUI displayText;
        [SerializeField] private Image sobaImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("移動設定")]
        [SerializeField] private float moveSpeed = 100f;
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private Vector3 endPosition;

        [Header("状態")]
        [SerializeField] private bool isActive = false;
        [SerializeField] private bool isMoving = false;
        [SerializeField] private WordData currentWord;

        [Header("視覚効果")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color activeColor = Color.yellow;
        [SerializeField] private Color typedColor = Color.gray;
        [SerializeField] private Color missColor = Color.red;

        private RectTransform rectTransform;
        private float missFlashTimer = 0f;
        private const float MISS_FLASH_DURATION = 0.2f;

        // プロパティ
        public bool IsActive => isActive;
        public bool IsMoving => isMoving;
        public WordData CurrentWord => currentWord;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Update()
        {
            if (isMoving)
            {
                MoveSoba();
            }

            if (isActive)
            {
                UpdateTypingDisplay();
            }

            if (missFlashTimer > 0)
            {
                missFlashTimer -= Time.deltaTime;
                if (missFlashTimer <= 0)
                {
                    ResetColor();
                }
            }
        }

        /// <summary>
        /// 蕎麦を初期化
        /// </summary>
        public void Initialize(WordData word, float speed, Vector3 start, Vector3 end)
        {
            if (word == null || !word.IsValid())
            {
                Debug.LogError("SobaController: 無効なWordDataです");
                return;
            }

            currentWord = word;
            moveSpeed = speed;
            startPosition = start;
            endPosition = end;

            // テキスト設定
            if (romajiText != null)
            {
                romajiText.text = word.romajiText;
                romajiText.color = normalColor;
            }

            if (displayText != null)
            {
                displayText.text = word.displayText;
            }

            // 位置設定
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = startPosition;
            }

            // 状態初期化
            isActive = false;
            isMoving = true;
            canvasGroup.alpha = 1f;

            Debug.Log($"蕎麦初期化: {word.displayText} ({word.romajiText})");
        }

        /// <summary>
        /// この蕎麦をアクティブ化
        /// </summary>
        public void Activate()
        {
            if (isActive)
                return;

            isActive = true;

            // InputManagerに登録
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetTargetWord(currentWord.romajiText);
                InputManager.Instance.OnCorrectInput.AddListener(OnCorrectInput);
                InputManager.Instance.OnIncorrectInput.AddListener(OnIncorrectInput);
                InputManager.Instance.OnWordCompleted.AddListener(OnWordCompleted);
            }

            // 視覚的フィードバック
            if (sobaImage != null)
            {
                sobaImage.color = activeColor;
            }

            Debug.Log($"蕎麦アクティブ化: {currentWord.romajiText}");
        }

        /// <summary>
        /// この蕎麦を非アクティブ化
        /// </summary>
        public void Deactivate()
        {
            if (!isActive)
                return;

            isActive = false;

            // InputManagerから登録解除
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCorrectInput.RemoveListener(OnCorrectInput);
                InputManager.Instance.OnIncorrectInput.RemoveListener(OnIncorrectInput);
                InputManager.Instance.OnWordCompleted.RemoveListener(OnWordCompleted);
            }

            // 色をリセット
            ResetColor();
        }

        /// <summary>
        /// 蕎麦を消去
        /// </summary>
        public void Clear(bool success)
        {
            Deactivate();
            isMoving = false;

            if (success)
            {
                // 成功時のアニメーション
                StartCoroutine(FadeOut());
            }
            else
            {
                // 失敗時は即座に非表示
                gameObject.SetActive(false);
            }
        }

        private void MoveSoba()
        {
            if (rectTransform == null)
                return;

            // 左へ移動
            Vector3 currentPos = rectTransform.anchoredPosition;
            currentPos.x -= moveSpeed * Time.deltaTime;
            rectTransform.anchoredPosition = currentPos;

            // 画面外に出たかチェック
            if (currentPos.x <= endPosition.x)
            {
                OnSobaReachedEnd();
            }
        }

        private void UpdateTypingDisplay()
        {
            if (InputManager.Instance == null || romajiText == null)
                return;

            string target = currentWord.romajiText;
            int position = InputManager.Instance.CurrentPosition;

            // タイプ済み部分と未タイプ部分を色分け
            string typedPart = target.Substring(0, position);
            string remainingPart = target.Substring(position);

            romajiText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(typedColor)}>{typedPart}</color>" +
                             $"<color=#{ColorUtility.ToHtmlStringRGB(normalColor)}>{remainingPart}</color>";
        }

        private void OnCorrectInput(char c)
        {
            // 正解時のフィードバック（必要に応じて実装）
        }

        private void OnIncorrectInput(char c)
        {
            // ミス時のフィードバック
            if (sobaImage != null)
            {
                sobaImage.color = missColor;
                missFlashTimer = MISS_FLASH_DURATION;
            }
        }

        private void OnWordCompleted(string word)
        {
            if (word != currentWord.romajiText)
                return;

            // GameManagerに通知
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnWordCleared(currentWord);
            }

            // 蕎麦を消去
            Clear(true);
        }

        private void OnSobaReachedEnd()
        {
            // 画面外に到達（失敗）
            if (isActive)
            {
                // コンボリセット
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnMissType();
                }
            }

            Clear(false);
        }

        private void ResetColor()
        {
            if (sobaImage != null)
            {
                sobaImage.color = isActive ? activeColor : normalColor;
            }
        }

        private System.Collections.IEnumerator FadeOut()
        {
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / duration);
                canvasGroup.alpha = alpha;

                // 上に移動しながらフェードアウト
                Vector3 pos = rectTransform.anchoredPosition;
                pos.y += 100f * Time.deltaTime;
                rectTransform.anchoredPosition = pos;

                yield return null;
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            // イベントのクリーンアップ
            Deactivate();
        }
    }
}
