using UnityEngine;
using System.Collections.Generic;

namespace Sobada.Utilities
{
    /// <summary>
    /// 音声管理クラス
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("BGM")]
        [SerializeField] private AudioClip titleBGM;
        [SerializeField] private AudioClip gameBGM;
        [SerializeField] private AudioClip resultBGM;

        [Header("SFX")]
        [SerializeField] private AudioClip typeSound;
        [SerializeField] private AudioClip correctSound;
        [SerializeField] private AudioClip missSound;
        [SerializeField] private AudioClip wordCompleteSound;
        [SerializeField] private AudioClip comboSound;
        [SerializeField] private AudioClip phaseChangeSound;

        [Header("Volume Settings")]
        [SerializeField] [Range(0f, 1f)] private float bgmVolume = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.7f;

        private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            InitializeSFXDictionary();
        }

        private void InitializeAudioSources()
        {
            if (bgmSource == null)
            {
                GameObject bgmObj = new GameObject("BGM Source");
                bgmObj.transform.SetParent(transform);
                bgmSource = bgmObj.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFX Source");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            UpdateVolumes();
        }

        private void InitializeSFXDictionary()
        {
            if (typeSound != null) sfxDictionary["type"] = typeSound;
            if (correctSound != null) sfxDictionary["correct"] = correctSound;
            if (missSound != null) sfxDictionary["miss"] = missSound;
            if (wordCompleteSound != null) sfxDictionary["wordComplete"] = wordCompleteSound;
            if (comboSound != null) sfxDictionary["combo"] = comboSound;
            if (phaseChangeSound != null) sfxDictionary["phaseChange"] = phaseChangeSound;
        }

        /// <summary>
        /// BGMを再生
        /// </summary>
        public void PlayBGM(string bgmName)
        {
            AudioClip clip = null;

            switch (bgmName.ToLower())
            {
                case "title":
                    clip = titleBGM;
                    break;
                case "game":
                    clip = gameBGM;
                    break;
                case "result":
                    clip = resultBGM;
                    break;
            }

            if (clip != null && bgmSource != null)
            {
                if (bgmSource.clip == clip && bgmSource.isPlaying)
                    return;

                bgmSource.clip = clip;
                bgmSource.Play();
            }
        }

        /// <summary>
        /// BGMを停止
        /// </summary>
        public void StopBGM()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        /// <summary>
        /// BGMをフェードアウト
        /// </summary>
        public void FadeOutBGM(float duration = 1f)
        {
            if (bgmSource != null)
            {
                StartCoroutine(FadeOutCoroutine(duration));
            }
        }

        /// <summary>
        /// SFXを再生
        /// </summary>
        public void PlaySFX(string sfxName)
        {
            if (sfxDictionary.TryGetValue(sfxName, out AudioClip clip))
            {
                if (clip != null && sfxSource != null)
                {
                    sfxSource.PlayOneShot(clip);
                }
            }
        }

        /// <summary>
        /// SFXを再生（AudioClip直接指定）
        /// </summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// タイプ音を再生
        /// </summary>
        public void PlayTypeSound()
        {
            PlaySFX("type");
        }

        /// <summary>
        /// 正解音を再生
        /// </summary>
        public void PlayCorrectSound()
        {
            PlaySFX("correct");
        }

        /// <summary>
        /// ミス音を再生
        /// </summary>
        public void PlayMissSound()
        {
            PlaySFX("miss");
        }

        /// <summary>
        /// 単語完成音を再生
        /// </summary>
        public void PlayWordCompleteSound()
        {
            PlaySFX("wordComplete");
        }

        /// <summary>
        /// コンボ音を再生
        /// </summary>
        public void PlayComboSound()
        {
            PlaySFX("combo");
        }

        /// <summary>
        /// フェーズ変更音を再生
        /// </summary>
        public void PlayPhaseChangeSound()
        {
            PlaySFX("phaseChange");
        }

        /// <summary>
        /// BGM音量を設定
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            if (bgmSource != null)
            {
                bgmSource.volume = bgmVolume;
            }
        }

        /// <summary>
        /// SFX音量を設定
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
            }
        }

        /// <summary>
        /// 全ての音量を更新
        /// </summary>
        private void UpdateVolumes()
        {
            SetBGMVolume(bgmVolume);
            SetSFXVolume(sfxVolume);
        }

        private System.Collections.IEnumerator FadeOutCoroutine(float duration)
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            bgmSource.volume = 0f;
            bgmSource.Stop();
            bgmSource.volume = startVolume;
        }
    }
}
