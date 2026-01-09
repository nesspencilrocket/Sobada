using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Sobada.Data
{
    /// <summary>
    /// フェーズごとの単語グループを管理するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "WordGroupData", menuName = "Sobada/Word Group Data", order = 2)]
    public class WordGroupData : ScriptableObject
    {
        [Header("グループ情報")]
        [Tooltip("グループ名")]
        public string groupName;

        [Tooltip("グループの説明")]
        [TextArea(2, 4)]
        public string description;

        [Header("単語リスト")]
        [Tooltip("このグループに含まれる単語")]
        public List<WordData> words = new List<WordData>();

        [Header("フィルタ設定")]
        [Tooltip("最小文字数")]
        public int minWordLength = 0;

        [Tooltip("最大文字数")]
        public int maxWordLength = 100;

        private List<WordData> usedWords = new List<WordData>();

        /// <summary>
        /// ランダムに単語を1つ取得
        /// </summary>
        public WordData GetRandomWord()
        {
            if (words == null || words.Count == 0)
            {
                Debug.LogError($"WordGroupData '{name}': 単語が登録されていません");
                return null;
            }

            // 有効な単語のみをフィルタリング
            var validWords = words.Where(w => w != null && w.IsValid()).ToList();
            
            if (validWords.Count == 0)
            {
                Debug.LogError($"WordGroupData '{name}': 有効な単語がありません");
                return null;
            }

            return validWords[Random.Range(0, validWords.Count)];
        }

        /// <summary>
        /// 重複を避けてランダムに単語を取得
        /// </summary>
        public WordData GetRandomWordNoDuplicate()
        {
            var validWords = words.Where(w => w != null && w.IsValid() && !usedWords.Contains(w)).ToList();

            // 全て使い切った場合はリセット
            if (validWords.Count == 0)
            {
                usedWords.Clear();
                validWords = words.Where(w => w != null && w.IsValid()).ToList();
            }

            if (validWords.Count == 0)
            {
                Debug.LogError($"WordGroupData '{name}': 有効な単語がありません");
                return null;
            }

            var selectedWord = validWords[Random.Range(0, validWords.Count)];
            usedWords.Add(selectedWord);
            return selectedWord;
        }

        /// <summary>
        /// 指定難易度の単語を取得
        /// </summary>
        public List<WordData> GetWordsByDifficulty(int difficulty)
        {
            return words.Where(w => w != null && w.IsValid() && w.difficulty == difficulty).ToList();
        }

        /// <summary>
        /// 文字数範囲内の単語を取得
        /// </summary>
        public List<WordData> GetWordsByLength(int minLength, int maxLength)
        {
            return words.Where(w => w != null && w.IsValid() && 
                               w.GetLength() >= minLength && 
                               w.GetLength() <= maxLength).ToList();
        }

        /// <summary>
        /// 単語の総数を取得
        /// </summary>
        public int GetWordCount()
        {
            return words.Count(w => w != null && w.IsValid());
        }

        /// <summary>
        /// 使用履歴をリセット
        /// </summary>
        public void ResetUsedWords()
        {
            usedWords.Clear();
        }

        /// <summary>
        /// データの妥当性を検証
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(groupName))
            {
                Debug.LogError($"WordGroupData '{name}': groupNameが空です");
                return false;
            }

            if (words == null || words.Count == 0)
            {
                Debug.LogError($"WordGroupData '{name}': 単語が登録されていません");
                return false;
            }

            int validCount = words.Count(w => w != null && w.IsValid());
            if (validCount == 0)
            {
                Debug.LogError($"WordGroupData '{name}': 有効な単語がありません");
                return false;
            }

            return true;
        }

        private void OnValidate()
        {
            // 重複チェック
            if (words != null && words.Count > 0)
            {
                var duplicates = words.GroupBy(w => w).Where(g => g.Count() > 1).Select(g => g.Key);
                foreach (var duplicate in duplicates)
                {
                    if (duplicate != null)
                    {
                        Debug.LogWarning($"WordGroupData '{name}': 単語 '{duplicate.name}' が重複しています");
                    }
                }
            }
        }
    }
}
