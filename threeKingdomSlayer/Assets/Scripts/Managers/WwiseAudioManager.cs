using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wwise 音频管理器 — 单例
/// 负责 Bank 加载/卸载、Event 播放、场景切换时的 Bank 生命周期管理
/// </summary>
public class WwiseAudioManager : MonoBehaviour
{
    public static WwiseAudioManager Instance { get; private set; }

    [Header("BGM Bank")]
    [SerializeField] private string bgmBankName = "Stage1_Bgm_Play.bnk";
    [SerializeField] private string bgmEventName = "Stage1_Bgm1_Play";

    private Dictionary<string, uint> _loadedBanks = new Dictionary<string, uint>();
    private uint _currentBgmPlayingID;
    private bool _bgmStopped;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        ApplySavedVolume();
    }

    private void Start()
    {
        LoadBank(bgmBankName);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            UnloadAllBanks();
            Instance = null;
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == "Battle")
        {
            StopBGM();
        }
    }

    #region Bank 管理

    public void LoadBank(string bankName)
    {
        if (_loadedBanks.ContainsKey(bankName))
        {
            Debug.LogWarning($"[WwiseAudio] Bank 已加载: {bankName}");
            return;
        }

        AKRESULT result = AkSoundEngine.LoadBank(bankName, out uint bankID);
        if (result == AKRESULT.AK_Success)
        {
            _loadedBanks[bankName] = bankID;
            Debug.Log($"[WwiseAudio] Bank 加载成功: {bankName} (ID={bankID})");
        }
        else
        {
            Debug.LogError($"[WwiseAudio] Bank 加载失败: {bankName} ({result})");
        }
    }

    public void UnloadBank(string bankName)
    {
        if (!_loadedBanks.TryGetValue(bankName, out uint bankID))
        {
            Debug.LogWarning($"[WwiseAudio] Bank 未加载: {bankName}");
            return;
        }

        AKRESULT result = AkSoundEngine.UnloadBank(bankID, System.IntPtr.Zero);
        if (result == AKRESULT.AK_Success)
        {
            _loadedBanks.Remove(bankName);
            Debug.Log($"[WwiseAudio] Bank 卸载成功: {bankName}");
        }
        else
        {
            Debug.LogError($"[WwiseAudio] Bank 卸载失败: {bankName} ({result})");
        }
    }

    private void UnloadAllBanks()
    {
        foreach (var bankName in new List<string>(_loadedBanks.Keys))
        {
            UnloadBank(bankName);
        }
    }

    #endregion

    #region Event 播放

    /// <summary>播放 BGM（2D，仅同时播放一首）</summary>
    public void PlayBGM(string eventName)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogWarning("[WwiseAudio] PlayBGM: eventName 为空");
            return;
        }

        StopBGM();
        _bgmStopped = false;

        _currentBgmPlayingID = AkSoundEngine.PostEvent(
            eventName,
            gameObject,
            (uint)AkCallbackType.AK_EndOfEvent,
            OnBgmEnded,
            null
        );

        Debug.Log($"[WwiseAudio] BGM 播放: {eventName} (playingID={_currentBgmPlayingID})");
    }

    public void StopBGM()
    {
        _bgmStopped = true;
        if (_currentBgmPlayingID != 0)
        {
            AkSoundEngine.StopPlayingID(_currentBgmPlayingID);
            _currentBgmPlayingID = 0;
        }
    }

    private void OnBgmEnded(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info)
    {
        if (_bgmStopped) return;

        if (!string.IsNullOrEmpty(bgmEventName))
        {
            _currentBgmPlayingID = AkSoundEngine.PostEvent(
                bgmEventName,
                gameObject
            );
        }
    }

    /// <summary>播放一次性 SFX Event</summary>
    public uint PostEvent(string eventName, GameObject go = null)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogWarning("[WwiseAudio] PostEvent: eventName 为空");
            return 0;
        }

        if (go == null) go = gameObject;

        uint playingID = AkSoundEngine.PostEvent(eventName, go);
        Debug.Log($"[WwiseAudio] Event 播放: {eventName} (playingID={playingID})");
        return playingID;
    }

    /// <summary>停止指定 playingID 的 Event</summary>
    public void StopEvent(uint playingID, int fadeOutMs = 0)
    {
        if (playingID == 0) return;
        AkSoundEngine.ExecuteActionOnPlayingID(
            AkActionOnEventType.AkActionOnEventType_Stop,
            playingID,
            fadeOutMs * 1000
        );
    }

    #endregion

    #region 音量控制

    private const string VOLUME_KEY = "wwise_master_volume";
    private const string RTPC_NAME = "MasterVolume";
    private float _masterVolume = 1f;

    /// <summary>设置主音量 (0~1)</summary>
    public void SetMasterVolume(float normalized)
    {
        _masterVolume = Mathf.Clamp01(normalized);
        float rtpcValue = _masterVolume * 100f;
        AkSoundEngine.SetRTPCValue(RTPC_NAME, rtpcValue);
        PlayerPrefs.SetFloat(VOLUME_KEY, _masterVolume);
        PlayerPrefs.Save();
    }

    /// <summary>获取主音量 (0~1)</summary>
    public float GetMasterVolume()
    {
        return _masterVolume;
    }

    /// <summary>恢复上次保存的音量</summary>
    public void ApplySavedVolume()
    {
        float saved = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        SetMasterVolume(saved);
    }

    #endregion

    #region 便捷方法

    /// <summary>播放当前配置的默认 BGM</summary>
    public void PlayDefaultBGM()
    {
        PlayBGM(bgmEventName);
    }

    #endregion
}
