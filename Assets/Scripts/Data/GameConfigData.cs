using UnityEngine;
using System;
using System.Linq;

namespace Sobada.Data
{
    /// <summary>
    /// 難易度レベルの列挙型
    /// </summary>
    public enum DifficultyLevel
    {
        Beginner,   // 初級
        Advanced    // 上級
    }

    /// <summary>
    /// フェーズ設定
    /// </summary>
    [Serializable]
    public class PhaseConfig
    {
        [Header("フェーズ情報")]
        [Tooltip("フェーズ名")]
        public string phaseName = "フェーズ1";

        [Tooltip("このフェーズで使用する単語グループ")]
        public WordGroupData wordGroup;

        [Header("ゲームプレイ設定")]
        [Tooltip("蕎麦の移動速度（ピクセル/秒）")]
        public float sobaSpeed = 100f;

        [Tooltip("蕎麦の生成間隔（秒）")]
        public float spawnInterval = 3f;

        [Tooltip("次のフェーズへ移行するスコア（0の場合は時間で移行）")]
        public int transitionScore = 0;

        [Tooltip("このフェーズの継続時間（秒、0の場合はスコアで移行）")]
        public float phaseDuration = 20f;

        [Header("ビジュアル設定")]
        [Tooltip("このフェーズの背景色")]
        public Color backgroundColor = new Color(0.95f, 0.95f, 0.95f);

        /// <summary>
        /// 設定の妥当性を検証
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(phaseName))
            {
                Debug.LogError("PhaseConfig: phaseNameが空です");
                return false;
            }

            if (wordGroup == null)
            {
                Debug.LogError($"PhaseConfig '{phaseName}': wordGroupが設定されていません");
                return false;
            }

            if (!wordGroup.IsValid())
            {
                Debug.LogError($"PhaseConfig '{phaseName}': wordGroupが無効です");
                return false;
            }

            if (sobaSpeed <= 0)
            {
                Debug.LogError($"PhaseConfig '{phaseName}': sobaSpeedが0以下です");
                return false;
            }

            if (spawnInterval <= 0)
            {
                Debug.LogError($"PhaseConfig '{phaseName}': spawnIntervalが0以下です");
                return false;
            }

            if (transitionScore == 0 && phaseDuration <= 0)
            {
                Debug.LogError($"PhaseConfig '{phaseName}': transitionScoreとphaseDurationの両方が0です");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// ゲーム全体の設定を管理するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfigData", menuName = "Sobada/Game Config Data", order = 3)]
    public class GameConfigData : ScriptableObject
    {
        [Header("基本設定")]
        [Tooltip("設定名")]
        public string configName;

        [Tooltip("難易度レベル")]
        public DifficultyLevel difficultyLevel = DifficultyLevel.Beginner;

        [Tooltip("ゲーム全体の制限時間（秒）")]
        public float totalGameTime = 60f;

        [Header("フェーズ設定")]
        [Tooltip("フェーズ設定の配列")]
        public PhaseConfig[] phases;

        [Header("スコア設定")]
        [Tooltip("コンボ時のスコア倍率（1コンボごとに加算）")]
        public float comboMultiplier = 0.1f;

        [Tooltip("最大コンボ倍率")]
        public float maxComboMultiplier = 3f;

        [Tooltip("ミスタイプのペナルティ（スコア減少）")]
        public int missTypePenalty = 0;

        [Tooltip("タイムボーナスの有効/無効")]
        public bool timeBonus = true;

        [Tooltip("タイムボーナスの倍率")]
        public float timeBonusMultiplier = 1.5f;

        [Header("UI設定")]
        [Tooltip("スコア表示のアニメーション時間")]
        public float scoreAnimationDuration = 0.5f;

        [Tooltip("フェーズ切り替え時の演出時間")]
        public float phaseTransitionDuration = 2f;

        /// <summary>
        /// インデックスでフェーズを取得
        /// </summary>
        public PhaseConfig GetPhaseByIndex(int index)
        {
            if (phases == null || index < 0 || index >= phases.Length)
            {
                Debug.LogError($"GameConfigData '{name}': 無効なフェーズインデックス {index}");
                return null;
            }

            return phases[index];
        }

        /// <summary>
        /// フェーズの総数を取得
        /// </summary>
        public int GetPhaseCount()
        {
            return phases?.Length ?? 0;
        }

        /// <summary>
        /// スコアに応じた現在のフェーズを取得
        /// </summary>
        public int GetCurrentPhaseIndex(int currentScore)
        {
            if (phases == null || phases.Length == 0)
                return 0;

            for (int i = phases.Length - 1; i >= 0; i--)
            {
                if (phases[i].transitionScore > 0 && currentScore >= phases[i].transitionScore)
                {
                    return Mathf.Min(i + 1, phases.Length - 1);
                }
            }

            return 0;
        }

        /// <summary>
        /// 経過時間に応じた現在のフェーズを取得
        /// </summary>
        public int GetCurrentPhaseIndexByTime(float elapsedTime)
        {
            if (phases == null || phases.Length == 0)
                return 0;

            float accumulatedTime = 0f;
            for (int i = 0; i < phases.Length; i++)
            {
                accumulatedTime += phases[i].phaseDuration;
                if (elapsedTime < accumulatedTime)
                {
                    return i;
                }
            }

            return phases.Length - 1;
        }

        /// <summary>
        /// コンボ倍率を計算
        /// </summary>
        public float GetComboMultiplier(int comboCount)
        {
            float multiplier = 1f + (comboCount * comboMultiplier);
            return Mathf.Min(multiplier, maxComboMultiplier);
        }

        /// <summary>
        /// データの妥当性を検証
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(configName))
            {
                Debug.LogError($"GameConfigData '{name}': configNameが空です");
                return false;
            }

            if (totalGameTime <= 0)
            {
                Debug.LogError($"GameConfigData '{name}': totalGameTimeが0以下です");
                return false;
            }

            if (phases == null || phases.Length == 0)
            {
                Debug.LogError($"GameConfigData '{name}': フェーズが設定されていません");
                return false;
            }

            bool allPhasesValid = phases.All(p => p != null && p.IsValid());
            if (!allPhasesValid)
            {
                Debug.LogError($"GameConfigData '{name}': 無効なフェーズが含まれています");
                return false;
            }

            return true;
        }

        private void OnValidate()
        {
            // 値の範囲チェック
            totalGameTime = Mathf.Max(1f, totalGameTime);
            comboMultiplier = Mathf.Max(0f, comboMultiplier);
            maxComboMultiplier = Mathf.Max(1f, maxComboMultiplier);
            missTypePenalty = Mathf.Max(0, missTypePenalty);
            timeBonusMultiplier = Mathf.Max(1f, timeBonusMultiplier);
        }
    }
}
