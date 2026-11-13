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

    [Header("敌人攻击预警 SFX — 挥武器音（随机二选一）")]
    [Tooltip("敌人起手预警（抡招）时播的**挥武器音**。\n" +
             "⚠️ 这里原来错接了 QTE 答题 UI 音（QTE_block1）→ 敌人抡招「叮」一声，和 QTE 答题音一模一样（2026-11-05 修）。\n" +
             "素材见 Assets/SFX/enemy/SOURCES.md（Mixkit 免费许可）。留空 = 不播。")]
    [SerializeField] private AudioClip[] _enemyAttackSwingClips;
    [SerializeField, Range(0f, 4f)] private float _enemyAttackTelegraphVolume = 0.8f;

    [Header("玩家启用格挡 SFX")]
    [Tooltip("玩家**举起枪进入防御位**那一刻的音（不是招架、也不是格挡受击）。\n" +
             "留空 = 不播（当前素材还没定：用户想要「轻微的握持声」，待补）。\n" +
             "格挡**受击**音走 Guard_Block；招架音走 Player_Parry，两者与本条无关。")]
    [SerializeField] private AudioClip _defendReadyClip;
    [SerializeField, Range(0f, 4f)] private float _defendReadyVolume = 0.8f;

    [Header("普通敌人受击语音")]
    [SerializeField] private AudioClip[] _enemyHitClips;
    [SerializeField, Min(0f)] private float _enemyHitCooldown = 0.14f;

    [Header("基础攻击命中 SFX — 随机二选一")]
    [SerializeField] private AudioClip[] _slashHitClips;
    [SerializeField] private AudioClip[] _stabHitClips;

    [Header("Launch 命中 SFX — 随机二选一")]
    [SerializeField] private AudioClip[] _launchHitClips;

    [Header("重击命中 SFX（强化招式 / Heavy 反馈档）")]
    [Tooltip("留空 = **回退到普通斩击音**（当前行为）。美术的「重击音」交来后直接填这里即可生效，不用改代码。")]
    [SerializeField] private AudioClip[] _heavyHitClips;

    [Header("防御受击 SFX — 随机二选一")]
    [Tooltip("格挡**吸收伤害**时的音（PlayerGuard 触发）。\n" +
             "2026-11-05 用户定：直接复用 QTE_block1/2 随机播放。")]
    [SerializeField] private AudioClip[] _guardBlockClips;
    [SerializeField, Range(0f, 4f)] private float _guardBlockVolume = 1f;
    [SerializeField, Min(0f)] private float _guardBlockCooldown = 0.08f;

    private AudioSource _bgmMainSource;
    private AudioSource _bgmEnvSource;
    private AudioSource _sfxSource;
    /// <summary>变调音效专用音源（不与主音效源共用 ⇒ 改 pitch 不会影响其它音效）。</summary>
    private AudioSource _pitchedSource;

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
    private const float HEAVY_HIT_VOLUME = 1.35f;
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
    private int _lastHeavyHitIndex = -1;
    private int _lastGuardBlockIndex = -1;
private int _lastEnemySwingIndex = -1;
    private float _nextGuardBlockTime;

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
        PreloadClipArray(_enemyAttackSwingClips);
        PreloadClip(_defendReadyClip);
        PreloadClipArray(_enemyHitClips);
        PreloadClipArray(_slashHitClips);
        PreloadClipArray(_stabHitClips);
        PreloadClipArray(_launchHitClips);
        PreloadClipArray(_guardBlockClips);
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

    /// <summary>
    /// 播放默认 BGM。
    ///
    /// ⚠️ **已经在播就不重播**（2026-11-05 修）：
    ///   旧实现先 <c>StopBGM()</c> 再 <c>Play()</c> → 每进一个节点都从头重播一遍
    ///   （用户：「不应该根据节点切 bgm，除非有打点切换的需求」）→ 改成幂等。
    ///   真要切歌（打点）时用 <paramref name="forceRestart"/> = true。
    /// </summary>
    public void PlayDefaultBGM(bool forceRestart = false)
    {
        EnsureAudioSources();
        bool mainPlaying = _bgmMain != null && _bgmMainSource != null && _bgmMainSource.isPlaying;
        bool envPlaying = _bgmEnv != null && _bgmEnvSource != null && _bgmEnvSource.isPlaying;
        if (!forceRestart && (mainPlaying || envPlaying)) return;

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
        PostEvent(eventName, 1f, 1f);
    }

    /// <summary>
    /// 带**音高 / 音量**的事件派发（REQ-006 · 破防专属音）。
    /// ⚠ <c>AudioSource.PlayOneShot</c> 本身没有 pitch 参数 ⇒ 变调必须走**独立的音源**，
    ///   否则会连带改掉主音效源的音高（把其它正在播的音效一起变调）。
    /// ⚠ 只有需要变调的事件用这个重载；其余事件的音量仍由各自的档位决定。
    /// </summary>
    public void PostEvent(string eventName, float pitch, float volume)
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
            case "Enemy_AttackTelegraph":
                PlayRandom(_enemyAttackSwingClips, _enemyAttackTelegraphVolume, ref _lastEnemySwingIndex);
                break;
            case "Player_DefendReady":
                PlayOneShot(_defendReadyClip, _defendReadyVolume);
                break;
            case "Enemy_Hit":
                PlayEnemyHit();
                break;
            case "Slash_Hit":
                PlayRandom(_slashHitClips, SLASH_HIT_VOLUME, ref _lastSlashHitIndex);
                break;
            case "Slash_HeavyHit":
                PlayHeavyHit();
                break;
            case "Stab_Hit":
                PlayRandom(_stabHitClips, STAB_HIT_VOLUME, ref _lastStabHitIndex);
                break;
            case "Launch_Hit":
                PlayRandom(_launchHitClips, LAUNCH_HIT_VOLUME, ref _lastLaunchHitIndex);
                break;
            case "Guard_Block":
                PlayGuardBlock();
                break;
            case "Guard_Break":
                PlayGuardBreak(pitch, volume);
                break;
        }
    }

    /// <summary>
    /// **破防专属音**（REQ-006）：格挡音**降调** + 重击音叠一层更低的低频
    /// ⇒ 读成「被顶开」而不是普通格挡。
    /// 素材还没交 ⇒ 先用现有 clip 降调拼（<c>_guardBlockClips</c> / <c>_heavyHitClips</c>）。
    /// </summary>
    private void PlayGuardBreak(float pitch, float volume)
    {
        if (_guardBlockClips != null && _guardBlockClips.Length > 0)
        {
            int index = Random.Range(0, _guardBlockClips.Length);
            PlayOneShotPitched(_guardBlockClips[index], volume, pitch);
        }
        if (_heavyHitClips != null && _heavyHitClips.Length > 0)
        {
            int index = Random.Range(0, _heavyHitClips.Length);
            // 低频垫底：再降一档、音量略低
            PlayOneShotPitched(_heavyHitClips[index], volume * 0.7f, Mathf.Max(0.25f, pitch - 0.2f));
        }
    }

    /// <summary>
    /// 变调的一次性音效：走**专用音源**，不动主音效源的 pitch。
    /// </summary>
    private void PlayOneShotPitched(AudioClip clip, float volume, float pitch)
    {
        if (clip == null) return;
        EnsureAudioSources();
        if (_pitchedSource == null)
        {
            _pitchedSource = gameObject.AddComponent<AudioSource>();
            _pitchedSource.playOnAwake = false;
            _pitchedSource.loop = false;
            _pitchedSource.spatialBlend = 0f;
            if (_audioMixer != null)
            {
                var sfxGroups = _audioMixer.FindMatchingGroups("SFX");
                if (sfxGroups.Length > 0) _pitchedSource.outputAudioMixerGroup = sfxGroups[0];
            }
        }
        _pitchedSource.pitch = Mathf.Clamp(pitch, 0.25f, 3f);
        _pitchedSource.PlayOneShot(clip, Mathf.Clamp(volume, 0f, 4f));
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

    /// <summary>
    /// 防御受击（<see cref="PlayerGuard"/> 吸收伤害）时的冷兵器格挡音。
    /// 带冷却：连续被打时不会叠成一片噪音。
    /// </summary>
    private void PlayGuardBlock()
    {
        if (_guardBlockClips == null || _guardBlockClips.Length == 0) return;
        if (Time.time < _nextGuardBlockTime) return;
        _nextGuardBlockTime = Time.time + _guardBlockCooldown;
        PlayRandom(_guardBlockClips, _guardBlockVolume, ref _lastGuardBlockIndex);
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

    /// <summary>
    /// **重击命中音**（强化招式 / Heavy 反馈档）。
    /// 专用数组为空时**回退到普通斩击音** → 素材还没交时行为与现状完全一致（不静音、不回归）。
    /// </summary>
    public void PlayHeavyHit()
    {
        EnsureAudioSources();
        if (_heavyHitClips == null || _heavyHitClips.Length == 0)
        {
            PlayRandom(_slashHitClips, HEAVY_HIT_VOLUME, ref _lastSlashHitIndex);
            return;
        }
        PlayRandom(_heavyHitClips, HEAVY_HIT_VOLUME, ref _lastHeavyHitIndex);
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
