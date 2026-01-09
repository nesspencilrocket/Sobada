using UnityEngine;

namespace Sobada.Data
{
    /// <summary>
    /// 個別の単語データを保持するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "WordData", menuName = "Sobada/Word Data", order = 1)]
    public class WordData : ScriptableObject
    {
        [Header("単語情報")]
        [Tooltip("タイプする文字列（ローマ字）")]
        public string romajiText;

        [Tooltip("画面に表示される日本語テキスト")]
        public string displayText;

        [Header("スコア設定")]
        [Tooltip("この単語をクリアした時の獲得金額")]
        public int scoreValue = 100;

        [Tooltip("単語の難易度（1-5）")]
        [Range(1, 5)]
        public int difficulty = 1;

        /// <summary>
        /// 単語の文字数を取得
        /// </summary>
        public int GetLength()
        {
            return romajiText?.Length ?? 0;
        }

        /// <summary>
        /// データの妥当性を検証
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(romajiText))
            {
                Debug.LogError($"WordData '{name}': romajiTextが空です");
                return false;
            }

            if (string.IsNullOrEmpty(displayText))
            {
                Debug.LogError($"WordData '{name}': displayTextが空です");
                return false;
            }

            if (scoreValue <= 0)
            {
                Debug.LogError($"WordData '{name}': scoreValueが0以下です");
                return false;
            }

            return true;
        }

        private void OnValidate()
        {
            // エディタでの編集時に自動検証
            if (!string.IsNullOrEmpty(romajiText))
            {
                romajiText = romajiText.ToLower().Trim();
            }
        }
    }
}
