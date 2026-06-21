using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity 原生音频管理器 — 单例
/// 替代 WwiseAudioManager，使用 AudioSource + AudioListener.volume
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

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

    private AudioSource _bgmMainSource;
    private AudioSource _bgmEnvSource;
    private AudioSource _sfxSource;

    private const string VOLUME_KEY = "master_volume";
    private float _masterVolume = 1f;

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
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        ApplySavedVolume();
    }

    private void CreateAudioSources()
    {
        var sources = GetComponents<AudioSource>();
        if (sources.Length >= 3)
        {
            // 使用场景中预置的 AudioSource（按挂载顺序: BGM_Main, BGM_Env, SFX）
            _bgmMainSource = sources[0];
            _bgmEnvSource = sources[1];
            _sfxSource = sources[2];
        }
        else
        {
            // 回退：运行时创建
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
        StopBGM();

        if (_bgmMain != null)
        {
            _bgmMainSource.clip = _bgmMain;
            _bgmMainSource.Play();
        }

        if (_bgmEnv != null)
        {
            _bgmEnvSource.clip = _bgmEnv;
            _bgmEnvSource.Play();
        }
    }

    public void StopBGM()
    {
        _bgmMainSource.Stop();
        _bgmEnvSource.Stop();
    }

    #endregion

    #region SFX

    public void PostEvent(string eventName)
    {
        switch (eventName)
        {
            case "Player_Attack":
                PlayRandom(_attackVoices);
                PlayRandom(_attackTiles);
                break;
            case "Player_Parry":
                PlayOneShot(_parryClip);
                break;
            case "QTE_Block":
                PlayRandom(_qteBlockClips);
                break;
        }
    }

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        int idx = Random.Range(0, clips.Length);
        PlayOneShot(clips[idx]);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    #endregion

    #region 音量

    public void SetMasterVolume(float normalized)
    {
        _masterVolume = Mathf.Clamp01(normalized);
        AudioListener.volume = _masterVolume;
        PlayerPrefs.SetFloat(VOLUME_KEY, _masterVolume);
        PlayerPrefs.Save();
    }

    public float GetMasterVolume()
    {
        return _masterVolume;
    }

    public void ApplySavedVolume()
    {
        float saved = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        SetMasterVolume(saved);
    }

    #endregion
}
