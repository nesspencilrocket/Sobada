using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sobada.Data;
using Sobada.Managers;

namespace Sobada.UI
{
    /// <summary>
    /// タイトル画面のUI管理
    /// </summary>
    public class TitleUIController : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private Button beginnerButton;
        [SerializeField] private Button advancedButton;
        [SerializeField] private Button quitButton;

        [Header("ゲーム設定")]
        [SerializeField] private GameConfigData beginnerConfig;
        [SerializeField] private GameConfigData advancedConfig;

        [Header("タイトル表示")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("説明文")]
        [SerializeField] private TextMeshProUGUI descriptionText;

        private void Start()
        {
            // ボタンのイベント設定
            if (beginnerButton != null)
            {
                beginnerButton.onClick.AddListener(OnBeginnerButtonClicked);
            }

            if (advancedButton != null)
            {
                advancedButton.onClick.AddListener(OnAdvancedButtonClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitButtonClicked);
            }

            // GameManagerのイベントに登録
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged.AddListener(OnGameStateChanged);
            }

            // 初期表示
            SetupTitleDisplay();
        }

        private void SetupTitleDisplay()
        {
            if (titleText != null)
            {
                titleText.text = "蕎麦だ";
            }

            if (subtitleText != null)
            {
                subtitleText.text = "SOBADA - Typing Game";
            }

            if (descriptionText != null)
            {
                descriptionText.text = "流れてくる蕎麦に表示された単語をタイプしよう!\n" +
                                      "制限時間内にどれだけ稼げるか挑戦!";
            }
        }

        private void OnBeginnerButtonClicked()
        {
            if (beginnerConfig == null)
            {
                Debug.LogError("TitleUIController: beginnerConfigが設定されていません");
                return;
            }

            if (!beginnerConfig.IsValid())
            {
                Debug.LogError("TitleUIController: beginnerConfigが無効です");
                return;
            }

            StartGame(beginnerConfig);
        }

        private void OnAdvancedButtonClicked()
        {
            if (advancedConfig == null)
            {
                Debug.LogError("TitleUIController: advancedConfigが設定されていません");
                return;
            }

            if (!advancedConfig.IsValid())
            {
                Debug.LogError("TitleUIController: advancedConfigが無効です");
                return;
            }

            StartGame(advancedConfig);
        }

        private void OnQuitButtonClicked()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void StartGame(GameConfigData config)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame(config);
            }
        }

        private void OnGameStateChanged(GameState newState)
        {
            gameObject.SetActive(newState == GameState.Title);
        }

        private void OnDestroy()
        {
            // イベントのクリーンアップ
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged.RemoveListener(OnGameStateChanged);
            }
        }
    }
}
