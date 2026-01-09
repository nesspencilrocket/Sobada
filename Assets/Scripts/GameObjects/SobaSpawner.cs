using UnityEngine;
using System.Collections.Generic;
using Sobada.Data;
using Sobada.Managers;

namespace Sobada.GameObjects
{
    /// <summary>
    /// 蕎麦オブジェクトの生成を管理
    /// </summary>
    public class SobaSpawner : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private GameObject sobaPrefab;
        [SerializeField] private Transform sobaContainer;
        [SerializeField] private Canvas gameCanvas;

        [Header("生成設定")]
        [SerializeField] private Vector3 spawnPosition = new Vector3(800f, 0f, 0f);
        [SerializeField] private Vector3 endPosition = new Vector3(-800f, 0f, 0f);
        [SerializeField] private float spawnInterval = 3f;
        [SerializeField] private int maxActiveSobas = 5;

        [Header("Y座標のランダム範囲")]
        [SerializeField] private float minY = -200f;
        [SerializeField] private float maxY = 200f;

        [Header("オブジェクトプール")]
        [SerializeField] private int poolSize = 10;
        [SerializeField] private List<SobaController> sobaPool = new List<SobaController>();

        private float nextSpawnTime = 0f;
        private List<SobaController> activeSobas = new List<SobaController>();
        private SobaController currentTargetSoba = null;

        private void Start()
        {
            InitializePool();

            // GameManagerのイベントに登録
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged.AddListener(OnGameStateChanged);
                GameManager.Instance.OnPhaseChanged.AddListener(OnPhaseChanged);
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return;

            // 自動生成
            if (Time.time >= nextSpawnTime && activeSobas.Count < maxActiveSobas)
            {
                SpawnSoba();
                nextSpawnTime = Time.time + spawnInterval;
            }

            // アクティブな蕎麦の管理
            ManageActiveSobas();
        }

        /// <summary>
        /// オブジェクトプールを初期化
        /// </summary>
        private void InitializePool()
        {
            if (sobaPrefab == null)
            {
                Debug.LogError("SobaSpawner: sobaPrefabが設定されていません");
                return;
            }

            if (sobaContainer == null)
            {
                sobaContainer = transform;
            }

            for (int i = 0; i < poolSize; i++)
            {
                GameObject sobaObj = Instantiate(sobaPrefab, sobaContainer);
                sobaObj.SetActive(false);

                SobaController soba = sobaObj.GetComponent<SobaController>();
                if (soba != null)
                {
                    sobaPool.Add(soba);
                }
            }

            Debug.Log($"SobaSpawner: オブジェクトプール初期化完了 ({poolSize}個)");
        }

        /// <summary>
        /// 蕎麦を生成
        /// </summary>
        public void SpawnSoba()
        {
            // 現在のフェーズ設定を取得
            PhaseConfig phase = GameManager.Instance?.GetCurrentPhase();
            if (phase == null || phase.wordGroup == null)
            {
                Debug.LogWarning("SobaSpawner: フェーズ設定が無効です");
                return;
            }

            // プールから未使用の蕎麦を取得
            SobaController soba = GetPooledSoba();
            if (soba == null)
            {
                Debug.LogWarning("SobaSpawner: 利用可能な蕎麦がありません");
                return;
            }

            // ランダムな単語を取得
            WordData word = phase.wordGroup.GetRandomWordNoDuplicate();
            if (word == null)
            {
                Debug.LogWarning("SobaSpawner: 単語の取得に失敗しました");
                return;
            }

            // Y座標をランダム化
            Vector3 randomSpawnPos = spawnPosition;
            randomSpawnPos.y = Random.Range(minY, maxY);

            // 蕎麦を初期化
            soba.gameObject.SetActive(true);
            soba.Initialize(word, phase.sobaSpeed, randomSpawnPos, endPosition);

            activeSobas.Add(soba);

            // 最初の蕎麦は自動的にアクティブ化
            if (currentTargetSoba == null)
            {
                SetTargetSoba(soba);
            }

            Debug.Log($"蕎麦生成: {word.displayText}");
        }

        /// <summary>
        /// アクティブな蕎麦を管理
        /// </summary>
        private void ManageActiveSobas()
        {
            // 非アクティブになった蕎麦を削除
            activeSobas.RemoveAll(s => s == null || !s.gameObject.activeSelf);

            // ターゲット蕎麦が無効になった場合、次の蕎麦をターゲットに
            if (currentTargetSoba == null || !currentTargetSoba.IsActive)
            {
                SobaController nextSoba = GetNextTargetSoba();
                if (nextSoba != null)
                {
                    SetTargetSoba(nextSoba);
                }
            }
        }

        /// <summary>
        /// プールから未使用の蕎麦を取得
        /// </summary>
        private SobaController GetPooledSoba()
        {
            foreach (var soba in sobaPool)
            {
                if (soba != null && !soba.gameObject.activeSelf)
                {
                    return soba;
                }
            }

            // プールが足りない場合は拡張
            if (sobaPrefab != null && sobaContainer != null)
            {
                GameObject sobaObj = Instantiate(sobaPrefab, sobaContainer);
                sobaObj.SetActive(false);

                SobaController soba = sobaObj.GetComponent<SobaController>();
                if (soba != null)
                {
                    sobaPool.Add(soba);
                    Debug.Log("SobaSpawner: プールを拡張しました");
                    return soba;
                }
            }

            return null;
        }

        /// <summary>
        /// 次のターゲット蕎麦を取得
        /// </summary>
        private SobaController GetNextTargetSoba()
        {
            // 最も右にある（最も早く生成された）蕎麦を選択
            SobaController rightmostSoba = null;
            float maxX = float.MinValue;

            foreach (var soba in activeSobas)
            {
                if (soba != null && soba.IsMoving)
                {
                    RectTransform rt = soba.GetComponent<RectTransform>();
                    if (rt != null && rt.anchoredPosition.x > maxX)
                    {
                        maxX = rt.anchoredPosition.x;
                        rightmostSoba = soba;
                    }
                }
            }

            return rightmostSoba;
        }

        /// <summary>
        /// ターゲット蕎麦を設定
        /// </summary>
        private void SetTargetSoba(SobaController soba)
        {
            if (currentTargetSoba != null)
            {
                currentTargetSoba.Deactivate();
            }

            currentTargetSoba = soba;
            
            if (currentTargetSoba != null)
            {
                currentTargetSoba.Activate();
            }
        }

        /// <summary>
        /// 全ての蕎麦をクリア
        /// </summary>
        public void ClearAllSobas()
        {
            foreach (var soba in activeSobas)
            {
                if (soba != null)
                {
                    soba.Clear(false);
                }
            }

            activeSobas.Clear();
            currentTargetSoba = null;

            Debug.Log("SobaSpawner: 全ての蕎麦をクリアしました");
        }

        /// <summary>
        /// フェーズ設定を更新
        /// </summary>
        private void UpdatePhaseSettings(PhaseConfig phase)
        {
            if (phase == null)
                return;

            spawnInterval = phase.spawnInterval;

            Debug.Log($"SobaSpawner: フェーズ設定更新 - 間隔: {spawnInterval}秒");
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.Playing)
            {
                nextSpawnTime = Time.time + 1f; // 最初の蕎麦は1秒後
            }
            else if (newState == GameState.Result || newState == GameState.Title)
            {
                ClearAllSobas();
            }
        }

        private void OnPhaseChanged(int phaseIndex)
        {
            PhaseConfig phase = GameManager.Instance?.GetCurrentPhase();
            if (phase != null)
            {
                UpdatePhaseSettings(phase);
            }
        }

        private void OnDestroy()
        {
            // イベントのクリーンアップ
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged.RemoveListener(OnGameStateChanged);
                GameManager.Instance.OnPhaseChanged.RemoveListener(OnPhaseChanged);
            }
        }

        // エディタ用のギズモ表示
        private void OnDrawGizmosSelected()
        {
            if (gameCanvas == null)
                return;

            // 生成位置と終了位置を可視化
            Gizmos.color = Color.green;
            Vector3 worldSpawnPos = transform.TransformPoint(spawnPosition);
            Gizmos.DrawWireSphere(worldSpawnPos, 20f);

            Gizmos.color = Color.red;
            Vector3 worldEndPos = transform.TransformPoint(endPosition);
            Gizmos.DrawWireSphere(worldEndPos, 20f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(worldSpawnPos, worldEndPos);
        }
    }
}
