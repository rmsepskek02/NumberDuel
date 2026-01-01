using Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 게임의 사운드를 관리하는 매니저
    /// BGM과 SFX를 구분하여 재생하고, 3단계 볼륨 시스템(Master, BGM, SFX) 제공
    /// DontDestroy 패턴으로 모든 씬에서 사용 가능
    /// </summary>
    public class SoundManager : SingletonDontDestroy<SoundManager>
    {
        #region Fields and Properties
        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private List<AudioSource> sfxSources = new List<AudioSource>();

        [Header("Settings")]
        [SerializeField] private int sfxPoolSize = 10; // SFX 오디오 소스 풀 크기

        // 오디오 클립 캐시 (ResourcesManager 패턴 참고)
        private Dictionary<SoundType, AudioClip> audioClipCache = new Dictionary<SoundType, AudioClip>();

        // 3단계 볼륨 시스템
        private float masterVolume = 1.0f;  // 마스터 볼륨 (0~1)
        private float bgmVolume = 0.7f;     // BGM 볼륨 (0~1)
        private float sfxVolume = 1.0f;     // SFX 볼륨 (0~1)

        // 3단계 음소거 시스템
        private bool isMasterMuted = false;  // 마스터 음소거
        private bool isBGMMuted = false;     // BGM 음소거
        private bool isSFXMuted = false;     // SFX 음소거

        // PlayerPrefs 키
        private const string PREF_MASTER_VOLUME = "Sound_MasterVolume";
        private const string PREF_BGM_VOLUME = "Sound_BGMVolume";
        private const string PREF_SFX_VOLUME = "Sound_SFXVolume";
        private const string PREF_MASTER_MUTE = "Sound_MasterMute";
        private const string PREF_BGM_MUTE = "Sound_BGMMute";
        private const string PREF_SFX_MUTE = "Sound_SFXMute";

        // 현재 재생 중인 BGM
        private SoundType? currentBGM = null;

        // 씬별 BGM 볼륨 스케일 (기본값 1.0)
        private Dictionary<string, float> sceneBGMVolumeScales = new Dictionary<string, float>()
        {
            { "SplashScene", 1.0f },
            { "JoinScene", 1.0f },
            { "LobbyScene", 1.0f },
            { "GameScene", 0.25f },  // GameScene은 25%로 낮춤
        };

        // 현재 씬의 BGM 볼륨 스케일
        private float currentSceneVolumeScale = 1.0f;

        /// <summary>
        /// 외부에서 볼륨값 읽기
        /// </summary>
        public float MasterVolume => masterVolume;
        public float BGMVolume => bgmVolume;
        public float SFXVolume => sfxVolume;

        /// <summary>
        /// 외부에서 음소거 상태 읽기
        /// </summary>
        public bool IsMasterMuted => isMasterMuted;
        public bool IsBGMMuted => isBGMMuted;
        public bool IsSFXMuted => isSFXMuted;

        /// <summary>
        /// 실제 적용되는 볼륨 (Master * Category * Mute * SceneScale)
        /// 음소거 시 0, 아니면 원래 볼륨 * 씬별 스케일
        /// </summary>
        private float EffectiveBGMVolume => (isMasterMuted || isBGMMuted) ? 0f : (masterVolume * bgmVolume * currentSceneVolumeScale);
        private float EffectiveSFXVolume => (isMasterMuted || isSFXMuted) ? 0f : (masterVolume * sfxVolume);
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

            // BGM AudioSource 생성
            if (bgmSource == null)
            {
                GameObject bgmObject = new GameObject("BGM_AudioSource");
                bgmObject.transform.SetParent(transform);
                bgmSource = bgmObject.AddComponent<AudioSource>();

                // 기본 설정
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;

                // ★ BGM 최적화 설정
                bgmSource.priority = 64; // BGM은 높은 우선순위
                bgmSource.spatialBlend = 0f; // 2D 사운드
            }

            // SFX AudioSource 풀 생성
            CreateSFXPool();

            // 저장된 설정 로드
            LoadSettings();

            // 볼륨 적용
            ApplyVolume();

            // 씬 전환 이벤트 등록
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

            // 백그라운드에서 사운드 사전 로드 (메인 스레드 블로킹 방지)
            StartCoroutine(PreloadSoundsAsync());
        }

        protected override void OnDestroy()
        {
            // 씬 전환 이벤트 해제
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

            ClearCache();

            // 베이스 클래스의 OnDestroy 호출 (싱글톤 인스턴스 정리)
            base.OnDestroy();
        }

        /// <summary>
        /// 씬 로드 시 자동으로 해당 씬의 BGM 재생
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // 씬별 볼륨 스케일 먼저 설정 (PlayBGM이 이 값을 사용함)
            currentSceneVolumeScale = sceneBGMVolumeScales.ContainsKey(scene.name)
                ? sceneBGMVolumeScales[scene.name]
                : 1.0f;

            SoundType? bgmToPlay = GetBGMForScene(scene.name);

            if (bgmToPlay.HasValue)
            {
                // BGM 재생 (FadeIn에서 currentSceneVolumeScale이 적용된 EffectiveBGMVolume 사용)
                PlayBGM(bgmToPlay.Value, loop: true, fadeInDuration: 0.5f);

                // 에디터에서 AudioListener 볼륨 확인
                #if UNITY_EDITOR
                Debug.Log($"[SoundManager] Scene: {scene.name}, Volume Scale: {currentSceneVolumeScale}");
                Debug.Log($"[SoundManager] AudioListener.volume: {AudioListener.volume}");
                Debug.Log($"[SoundManager] BGM AudioSource volume: {bgmSource.volume}");
                Debug.Log($"[SoundManager] Master: {masterVolume}, BGM: {bgmVolume}, Muted: Master={isMasterMuted}, BGM={isBGMMuted}");
                Debug.Log($"[SoundManager] Effective BGM Volume: {EffectiveBGMVolume}");
                #endif
            }
        }

        /// <summary>
        /// 씬 이름에 따라 재생할 BGM 반환
        /// </summary>
        private SoundType? GetBGMForScene(string sceneName)
        {
            return sceneName switch
            {
                "SplashScene" => SoundType.BGM_Splash,
                "JoinScene" => SoundType.BGM_Splash,      // 로그인 화면도 Splash BGM 사용
                "LobbyScene" => SoundType.BGM_Lobby,
                "GameScene" => SoundType.BGM_Battle,
                _ => null  // 알 수 없는 씬은 BGM 재생 안 함
            };
        }
        #endregion

        #region Audio Source Pool Management
        /// <summary>
        /// SFX용 AudioSource 풀 생성
        /// AudioSource 최적화 설정 포함
        /// </summary>
        private void CreateSFXPool()
        {
            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject sfxObject = new GameObject($"SFX_AudioSource_{i}");
                sfxObject.transform.SetParent(transform);
                AudioSource source = sfxObject.AddComponent<AudioSource>();

                // 기본 설정
                source.loop = false;
                source.playOnAwake = false;

                // ★ 성능 최적화 설정
                source.priority = 128; // 기본 우선순위 (0=최고, 256=최저)
                source.spatialBlend = 0f; // 2D 사운드 (3D 계산 비활성화)
                source.volume = 1f; // 기본 볼륨

                sfxSources.Add(source);
            }
        }

        /// <summary>
        /// 사용 가능한 SFX AudioSource 찾기
        /// </summary>
        private AudioSource GetAvailableSFXSource()
        {
            foreach (var source in sfxSources)
            {
                if (!source.isPlaying)
                    return source;
            }

            // 모든 소스가 사용 중이면 첫 번째 반환 (덮어씀)
            return sfxSources[0];
        }
        #endregion

        #region Volume Management
        /// <summary>
        /// 마스터 볼륨 설정 (0~1) - 자동 저장
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolume();
            SaveSettings();
        }

        /// <summary>
        /// BGM 볼륨 설정 (0~1) - 자동 저장
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            ApplyVolume();
            SaveSettings();
        }

        /// <summary>
        /// SFX 볼륨 설정 (0~1) - 자동 저장
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyVolume();
            SaveSettings();
        }

        /// <summary>
        /// 마스터 음소거 토글 - 자동 저장
        /// </summary>
        public void SetMasterMute(bool mute)
        {
            isMasterMuted = mute;
            ApplyVolume();
            SaveSettings();
        }

        /// <summary>
        /// BGM 음소거 토글 - 자동 저장
        /// </summary>
        public void SetBGMMute(bool mute)
        {
            isBGMMuted = mute;
            ApplyVolume();
            SaveSettings();
        }

        /// <summary>
        /// SFX 음소거 토글 - 자동 저장
        /// </summary>
        public void SetSFXMute(bool mute)
        {
            isSFXMuted = mute;
            ApplyVolume();
            SaveSettings();
        }

        /// <summary>
        /// 모든 AudioSource에 볼륨 적용
        /// </summary>
        private void ApplyVolume()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = EffectiveBGMVolume;
            }

            foreach (var source in sfxSources)
            {
                if (source != null)
                {
                    source.volume = EffectiveSFXVolume;
                }
            }
        }
        #endregion

        #region Settings Management
        /// <summary>
        /// 설정 저장 (자동 호출됨)
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, masterVolume);
            PlayerPrefs.SetFloat(PREF_BGM_VOLUME, bgmVolume);
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, sfxVolume);
            PlayerPrefs.SetInt(PREF_MASTER_MUTE, isMasterMuted ? 1 : 0);
            PlayerPrefs.SetInt(PREF_BGM_MUTE, isBGMMuted ? 1 : 0);
            PlayerPrefs.SetInt(PREF_SFX_MUTE, isSFXMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 설정 로드
        /// </summary>
        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1.0f);
            bgmVolume = PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 0.7f);
            sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1.0f);
            isMasterMuted = PlayerPrefs.GetInt(PREF_MASTER_MUTE, 0) == 1;
            isBGMMuted = PlayerPrefs.GetInt(PREF_BGM_MUTE, 0) == 1;
            isSFXMuted = PlayerPrefs.GetInt(PREF_SFX_MUTE, 0) == 1;
        }
        #endregion

        #region Audio Clip Management
        /// <summary>
        /// 백그라운드에서 자주 사용하는 사운드를 비동기로 사전 로드
        /// 메인 스레드 블로킹을 방지하여 부드러운 게임 시작 보장
        /// Awake()에서 자동으로 호출됨
        /// </summary>
        private IEnumerator PreloadSoundsAsync()
        {
            // 첫 프레임이 렌더링된 후 실행 (로딩 화면이 먼저 표시되도록)
            yield return new WaitForEndOfFrame();

            // 자주 사용하는 사운드 로드
            PreloadFrequentSounds();
        }

        /// <summary>
        /// 자주 사용되는 AudioClip들을 미리 로드
        /// 사운드 재생 지연을 방지하기 위한 사전 로딩
        /// PreloadSoundsAsync()에서 호출됨
        /// </summary>
        private void PreloadFrequentSounds()
        {
            // 카드 관련 사운드 (가장 자주 사용)
            PreloadSingleSound(SoundType.Card_Draw);
            PreloadSingleSound(SoundType.Card_PlaceNormal);
            PreloadSingleSound(SoundType.Card_PlaceSecret);
            PreloadSingleSound(SoundType.Card_Attack);
            PreloadSingleSound(SoundType.Card_Destroy);

            // 전투 관련 사운드
            PreloadSingleSound(SoundType.Combat_Plus);
            PreloadSingleSound(SoundType.Combat_Minus);
            PreloadSingleSound(SoundType.Combat_Multiply);
            PreloadSingleSound(SoundType.Combat_Divide);
            PreloadSingleSound(SoundType.Combat_Damage);
            PreloadSingleSound(SoundType.Combat_SecretReveal);

            // 조커 관련 사운드
            PreloadSingleSound(SoundType.Joker_Draw);
            PreloadSingleSound(SoundType.Joker_Delete);
            PreloadSingleSound(SoundType.Joker_Swap);

            // UI 사운드
            PreloadSingleSound(SoundType.UI_ButtonClick);
            PreloadSingleSound(SoundType.UI_TurnStart);
        }

        /// <summary>
        /// 단일 사운드 사전 로드
        /// </summary>
        private void PreloadSingleSound(SoundType soundType)
        {
            if (audioClipCache.ContainsKey(soundType))
                return; // 이미 로드됨

            LoadAudioClip(soundType);
        }

        /// <summary>
        /// AudioClip 로드 및 캐싱
        /// </summary>
        private AudioClip LoadAudioClip(SoundType soundType)
        {
            // 이미 캐시되어 있으면 반환
            if (audioClipCache.TryGetValue(soundType, out AudioClip cachedClip))
            {
                return cachedClip;
            }

            // SoundType을 경로와 파일명으로 변환
            string path = GetAudioPath(soundType);
            string fileName = soundType.ToString();

            try
            {
                AudioClip clip = Resources.Load<AudioClip>($"{path}/{fileName}");

                if (clip != null)
                {
                    audioClipCache[soundType] = clip;
                    return clip;
                }
                else
                {
                    Debug.LogError($"[SoundManager] AudioClip을 찾을 수 없습니다: {path}/{fileName}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SoundManager] AudioClip 로드 중 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// SoundType에 따른 리소스 경로 반환
        /// </summary>
        private string GetAudioPath(SoundType soundType)
        {
            string soundName = soundType.ToString();

            if (soundName.StartsWith("BGM_"))
                return "Audio/BGM";
            else if (soundName.StartsWith("UI_"))
                return "Audio/SFX/UI";
            else if (soundName.StartsWith("Card_"))
                return "Audio/SFX/Card";
            else if (soundName.StartsWith("Combat_"))
                return "Audio/SFX/Combat";
            else if (soundName.StartsWith("Joker_"))
                return "Audio/SFX/Joker";
            else if (soundName.StartsWith("Game_"))
                return "Audio/SFX/Game";

            return "Audio";
        }

        /// <summary>
        /// SoundType이 BGM인지 확인
        /// </summary>
        private bool IsBGM(SoundType soundType)
        {
            return soundType.ToString().StartsWith("BGM_");
        }

        /// <summary>
        /// 캐시 정리
        /// </summary>
        private void ClearCache()
        {
            audioClipCache.Clear();
        }
        #endregion

        #region Play Methods
        /// <summary>
        /// BGM 재생
        /// </summary>
        public void PlayBGM(SoundType bgmType, bool loop = true, float fadeInDuration = 0.5f)
        {
            if (!IsBGM(bgmType))
            {
                Debug.LogWarning($"[SoundManager] '{bgmType}'은 BGM 타입이 아닙니다.");
                return;
            }

            AudioClip clip = LoadAudioClip(bgmType);
            if (clip == null)
                return;

            // 이미 같은 BGM이 재생 중이면 무시
            if (currentBGM == bgmType && bgmSource.isPlaying)
                return;

            // 페이드 인 효과와 함께 재생
            currentBGM = bgmType;
            bgmSource.clip = clip;
            bgmSource.loop = loop;

            if (fadeInDuration > 0)
            {
                StartCoroutine(FadeIn(bgmSource, fadeInDuration));
            }
            else
            {
                bgmSource.volume = EffectiveBGMVolume;
                bgmSource.Play();
            }
        }

        /// <summary>
        /// BGM 정지
        /// </summary>
        public void StopBGM(float fadeOutDuration = 0.5f)
        {
            if (!bgmSource.isPlaying)
                return;

            if (fadeOutDuration > 0)
            {
                StartCoroutine(FadeOut(bgmSource, fadeOutDuration, () =>
                {
                    bgmSource.Stop();
                    currentBGM = null;
                }));
            }
            else
            {
                bgmSource.Stop();
                currentBGM = null;
            }
        }

        /// <summary>
        /// SFX 재생
        /// </summary>
        public void PlaySFX(SoundType sfxType, float volumeScale = 1.0f)
        {
            if (IsBGM(sfxType))
            {
                Debug.LogWarning($"[SoundManager] '{sfxType}'은 SFX 타입이 아닙니다.");
                return;
            }

            AudioClip clip = LoadAudioClip(sfxType);
            if (clip == null)
                return;

            AudioSource source = GetAvailableSFXSource();
            source.volume = EffectiveSFXVolume * Mathf.Clamp01(volumeScale);
            source.PlayOneShot(clip);
        }

        /// <summary>
        /// 모든 사운드 정지
        /// </summary>
        public void StopAllSounds()
        {
            StopBGM(0f);

            foreach (var source in sfxSources)
            {
                source.Stop();
            }
        }
        #endregion

        #region Fade Effects
        /// <summary>
        /// 페이드 인 효과
        /// </summary>
        private IEnumerator FadeIn(AudioSource source, float duration)
        {
            source.volume = 0f;
            source.Play();

            float elapsed = 0f;
            float targetVolume = source == bgmSource ? EffectiveBGMVolume : EffectiveSFXVolume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
                yield return null;
            }

            source.volume = targetVolume;
        }

        /// <summary>
        /// 페이드 아웃 효과
        /// </summary>
        private IEnumerator FadeOut(AudioSource source, float duration, Action onComplete = null)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            source.volume = 0f;
            onComplete?.Invoke();
        }
        #endregion
    }
}
