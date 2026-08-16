using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity 原生音频管理器 — 单例
/// 使用 AudioSource + AudioMixer 管理 BGM、音效和总音量
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer _audioMixer;

    [Header("BGM — 双层叠加")]
    [SerializeField] private AudioClip _bgmMain;
    [SerializeField] private AudioClip _bgmEnv;

    [Header("攻击语音 — 随机三选一")]
    [SerializeField] private AudioClip[] _attackVoices;

    [Header("攻击刀剑音 — 随机二选一")]
    [SerializeField] private AudioClip[] _attackTiles;

    [Header("格挡 SFX")]
    [SerializeField] private AudioClip _parryClip;

    [Header("QTE 格挡 SFX — 随机二选一")]
    [SerializeField] private AudioClip[] _qteBlockClips;
    [SerializeField, Range(0f, 4f)] private float _qteBlockVolume = 1.5f;

    [Header("普通敌人受击语音")]
    [SerializeField] private AudioClip[] _enemyHitClips;
    [SerializeField, Min(0f)] private float _enemyHitCooldown = 0.14f;

    [Header("基础攻击命中 SFX — 随机二选一")]
    [SerializeField] private AudioClip[] _slashHitClips;
    [SerializeField] private AudioClip[] _stabHitClips;

    [Header("Launch 命中 SFX — 随机二选一")]
    [SerializeField] private AudioClip[] _launchHitClips;

    private AudioSource _bgmMainSource;
    private AudioSource _bgmEnvSource;
    private AudioSource _sfxSource;

    private const string MASTER_VOLUME_KEY = "master_volume";
    private const string BGM_VOLUME_KEY = "bgm_volume";
    private const string SFX_VOLUME_KEY = "sfx_volume";
    private const string MASTER_VOLUME_PARAMETER = "MasterVolume";
    private const string BGM_VOLUME_PARAMETER = "BGMVolume";
    private const string SFX_VOLUME_PARAMETER = "SFXVolume";
    private const float ATTACK_VOICE_VOLUME = 0.5f;
    private const float SLASH_HIT_VOLUME = 1.25f;
    private const float STAB_HIT_VOLUME = 1.25f;
    private const float LAUNCH_HIT_VOLUME = 1.25f;
    private const float ENEMY_HIT_VOLUME = 0.8f;
    private const float MIN_VOLUME_DB = -80f;

    private float _masterVolume = 1f;
    private float _bgmVolume = 1f;
    private float _sfxVolume = 1f;
    private float _nextEnemyHitTime;
    private int _lastEnemyHitIndex = -1;
    private int _lastSlashHitIndex = -1;
    private int _lastStabHitIndex = -1;
    private int _lastLaunchHitIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateAudioSources();
        PreloadAllClips();
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        ApplySavedVolume();
    }

    private System.Collections.IEnumerator Start()
    {
        yield return null;
        ApplySavedVolume();
    }

    private void CreateAudioSources()
    {
        var sources = GetComponents<AudioSource>();
        if (sources.Length >= 3)
        {
            _bgmMainSource = sources[0];
            _bgmEnvSource = sources[1];
            _sfxSource = sources[2];
        }
        else
        {
            while (sources.Length < 3)
            {
                gameObject.AddComponent<AudioSource>();
                sources = GetComponents<AudioSource>();
            }
            _bgmMainSource = sources[sources.Length - 3];
            _bgmEnvSource = sources[sources.Length - 2];
            _sfxSource = sources[sources.Length - 1];
        }

        _bgmMainSource.playOnAwake = false;
        _bgmMainSource.loop = true;
        _bgmMainSource.spatialBlend = 0f;

        _bgmEnvSource.playOnAwake = false;
        _bgmEnvSource.loop = true;
        _bgmEnvSource.spatialBlend = 0f;

        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f;

        ApplyAudioMixerRouting();
    }

    private void ApplyAudioMixerRouting()
    {
        if (_audioMixer == null) return;
        var bgmGroups = _audioMixer.FindMatchingGroups("BGM");
        var sfxGroups = _audioMixer.FindMatchingGroups("SFX");
        if (bgmGroups.Length > 0)
        {
            _bgmMainSource.outputAudioMixerGroup = bgmGroups[0];
            _bgmEnvSource.outputAudioMixerGroup = bgmGroups[0];
        }
        if (sfxGroups.Length > 0)
            _sfxSource.outputAudioMixerGroup = sfxGroups[0];
    }

    private void PreloadAllClips()
    {
        PreloadClip(_bgmMain);
        PreloadClip(_bgmEnv);
        PreloadClipArray(_attackVoices);
        PreloadClipArray(_attackTiles);
        PreloadClip(_parryClip);
        PreloadClipArray(_qteBlockClips);
        PreloadClipArray(_enemyHitClips);
        PreloadClipArray(_slashHitClips);
        PreloadClipArray(_stabHitClips);
        PreloadClipArray(_launchHitClips);
    }

    private void PreloadClip(AudioClip clip)
    {
        if (clip != null && clip.loadType == AudioClipLoadType.DecompressOnLoad && clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
            Debug.Log($"[AudioManager] Preloading clip: {clip.name}");
        }
    }

    private void PreloadClipArray(AudioClip[] clips)
    {
        if (clips == null) return;
        foreach (var clip in clips)
            PreloadClip(clip);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Instance = null;
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == "Battle")
            StopBGM();
    }

    #region BGM

    public void PlayDefaultBGM()
    {
        EnsureAudioSources();
        StopBGM();

        if (_bgmMain != null && _bgmMainSource != null)
        {
            _bgmMainSource.clip = _bgmMain;
            _bgmMainSource.Play();
        }

        if (_bgmEnv != null && _bgmEnvSource != null)
        {
            _bgmEnvSource.clip = _bgmEnv;
            _bgmEnvSource.Play();
        }
    }

    public void StopBGM()
    {
        if (_bgmMainSource != null) _bgmMainSource.Stop();
        if (_bgmEnvSource != null) _bgmEnvSource.Stop();
    }

    #endregion

    #region SFX

    private void EnsureAudioSources()
    {
        if (_sfxSource == null || _bgmMainSource == null || _bgmEnvSource == null)
            CreateAudioSources();
    }

    public void PostEvent(string eventName)
    {
        EnsureAudioSources();
        switch (eventName)
        {
            case "Player_Attack":
                PlayRandom(_attackVoices, ATTACK_VOICE_VOLUME);
                PlayRandom(_attackTiles);
                break;
            case "Player_Parry":
                PlayOneShot(_parryClip);
                break;
            case "QTE_Block":
                PlayRandom(_qteBlockClips, _qteBlockVolume);
                break;
            case "Enemy_Hit":
                PlayEnemyHit();
                break;
            case "Slash_Hit":
                PlayRandom(_slashHitClips, SLASH_HIT_VOLUME, ref _lastSlashHitIndex);
                break;
            case "Stab_Hit":
                PlayRandom(_stabHitClips, STAB_HIT_VOLUME, ref _lastStabHitIndex);
                break;
            case "Launch_Hit":
                PlayRandom(_launchHitClips, LAUNCH_HIT_VOLUME, ref _lastLaunchHitIndex);
                break;
        }
    }

    private void PlayRandom(AudioClip[] clips, float volumeScale = 1f)
    {
        if (clips == null || clips.Length == 0) return;
        int idx = Random.Range(0, clips.Length);
        PlayOneShot(clips[idx], volumeScale);
    }

    private void PlayRandom(AudioClip[] clips, float volumeScale, ref int lastIndex)
    {
        if (clips == null || clips.Length == 0) return;
        int index = Random.Range(0, clips.Length);
        if (clips.Length > 1 && index == lastIndex)
            index = (index + Random.Range(1, clips.Length)) % clips.Length;
        lastIndex = index;
        PlayOneShot(clips[index], volumeScale);
    }

    private void PlayEnemyHit()
    {
        if (_enemyHitClips == null || _enemyHitClips.Length == 0) return;
        if (Time.time < _nextEnemyHitTime) return;

        int index = Random.Range(0, _enemyHitClips.Length);
        if (_enemyHitClips.Length > 1 && index == _lastEnemyHitIndex)
            index = (index + Random.Range(1, _enemyHitClips.Length)) % _enemyHitClips.Length;

        _lastEnemyHitIndex = index;
        _nextEnemyHitTime = Time.time + _enemyHitCooldown;
        PlayOneShot(_enemyHitClips[index], ENEMY_HIT_VOLUME);
    }

    public void PlaySlashHit()
    {
        EnsureAudioSources();
        PlayRandom(_slashHitClips, 1f, ref _lastSlashHitIndex);
    }

    public void PlayStabHit()
    {
        EnsureAudioSources();
        PlayRandom(_stabHitClips, 1f, ref _lastStabHitIndex);
    }

    private void PlayOneShot(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip, volumeScale);
    }

    #endregion

    #region 音量

    public void SetMasterVolume(float normalized)
    {
        _masterVolume = Mathf.Clamp01(normalized);
        ApplyMixerVolume(MASTER_VOLUME_PARAMETER, _masterVolume);
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, _masterVolume);
        PlayerPrefs.Save();
    }

    public void SetBgmVolume(float normalized)
    {
        _bgmVolume = Mathf.Clamp01(normalized);
        ApplyMixerVolume(BGM_VOLUME_PARAMETER, _bgmVolume);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, _bgmVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float normalized)
    {
        _sfxVolume = Mathf.Clamp01(normalized);
        ApplyMixerVolume(SFX_VOLUME_PARAMETER, _sfxVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, _sfxVolume);
        PlayerPrefs.Save();
    }

    public float GetMasterVolume() => _masterVolume;
    public float GetBgmVolume() => _bgmVolume;
    public float GetSfxVolume() => _sfxVolume;

    public void ApplySavedVolume()
    {
        _masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        _bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
        _sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        ApplyMixerVolume(MASTER_VOLUME_PARAMETER, _masterVolume);
        ApplyMixerVolume(BGM_VOLUME_PARAMETER, _bgmVolume);
        ApplyMixerVolume(SFX_VOLUME_PARAMETER, _sfxVolume);
        AudioListener.volume = 1f;
    }

    private void ApplyMixerVolume(string parameter, float normalized)
    {
        if (_audioMixer == null) return;
        float db = normalized <= 0.0001f ? MIN_VOLUME_DB : Mathf.Log10(normalized) * 20f;
        _audioMixer.SetFloat(parameter, db);
    }

    #endregion
}
