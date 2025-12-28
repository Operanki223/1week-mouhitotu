using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoundManager : MonoBehaviour
{
    [Header("Config UI")]
    [SerializeField] public GameObject _configPanel;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _audioBGM;
    [SerializeField] private List<AudioSource> _audioSE = new List<AudioSource>();

    [Header("Volumes")]
    [SerializeField, Range(0.0f, 1.0f)] private float volumeBGM = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)] private float volumeSE = 0.5f;

    [Header("Clips")]
    public List<AudioClip> _audioClipsSE = new List<AudioClip>();
    public List<AudioClip> _audioClipsBGM = new List<AudioClip>();

    public static SoundManager instance;
    public SceneName sceneName = SceneName.None;

    // 内部用：今流しているBGMとシーン
    private SceneName _currentBGMScene = SceneName.None;
    private AudioClip _currentBGMClip = null;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // BGM 初期設定
        if (_audioBGM != null)
        {
            _audioBGM.loop = true;          // BGMはループ
            _audioBGM.volume = volumeBGM;   // 初期ボリューム
        }

        // SE側のボリュームも初期値を反映
        ApplySEVolumeToAll();

        // 最初はパネル閉じてゲーム動作状態にしておく
        if (_configPanel != null)
        {
            _configPanel.SetActive(false);
        }
        Time.timeScale = 1f;
    }

    void Update()
    {
        // Qキーで設定パネルの開閉＋ポーズ切り替え（新Input System）
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame && !SoundManager.instance.sceneName.Equals(SceneName.Title))
        {
            ToggleConfigPanel();
        }

        // ※ 音量は SetBGMVolume / SetSEVolume からだけ変更する想定なので
        //    ここでは volume を触らない
    }

    // =========================
    // パネル & ポーズ制御
    // =========================

    /// <summary>
    /// 設定パネルを開いてゲームを止める
    /// </summary>
    public void OpenConfigPanel()
    {
        if (_configPanel == null) return;

        _configPanel.SetActive(true);
        Time.timeScale = 0f;

        // 開くときにSEを鳴らす（配列数チェック付き）
        if (_audioClipsSE.Count > 3)
        {
            PlaySE(_audioClipsSE[3]);
        }
    }

    /// <summary>
    /// 設定パネルを閉じてゲームを再開する
    /// （ボタンからも呼べるように public）
    /// </summary>
    public void CloseConfigPanel()
    {
        if (_configPanel == null) return;

        // ★ 配列数チェックを追加してから鳴らす
        if (_audioClipsSE.Count > 3)
        {
            PlaySE(_audioClipsSE[3]);
        }

        _configPanel.SetActive(false);
        Time.timeScale = 1f;
    }


    /// <summary>
    /// 設定パネルの開閉をトグルしてポーズを切り替える
    /// </summary>
    public void ToggleConfigPanel()
    {
        if (_configPanel == null) return;

        bool willOpen = !_configPanel.activeSelf;

        if (willOpen)
        {
            // ★ 開くときは共通の OpenConfigPanel を使う
            OpenConfigPanel();
        }
        else
        {
            // ★ 閉じるときは共通の CloseConfigPanel を使う
            CloseConfigPanel();
        }
    }


    // =========================
    // 音量まわり
    // =========================

    /// <summary>
    /// 全てのSE AudioSourceに volumeSE を適用
    /// </summary>
    private void ApplySEVolumeToAll()
    {
        foreach (var se in _audioSE)
        {
            if (se != null)
            {
                se.volume = volumeSE;
            }
        }
    }

    /// <summary>
    /// スライダーからBGM音量を変更する用
    /// Slider の OnValueChanged(float) から呼ぶ
    /// </summary>
    public void SetBGMVolume(float value)
    {
        volumeBGM = Mathf.Clamp01(value);

        if (_audioBGM != null)
        {
            _audioBGM.volume = volumeBGM;
        }
    }

    /// <summary>
    /// スライダーからSE音量を変更する用
    /// Slider の OnValueChanged(float) から呼ぶ
    /// </summary>
    public void SetSEVolume(float value)
    {
        volumeSE = Mathf.Clamp01(value);
        ApplySEVolumeToAll();
    }

    // =========================
    // BGM再生
    // =========================

    /// <summary>
    /// 指定したBGMをループ再生する
    /// </summary>
    public void PlayBGM(AudioClip audioClip)
    {
        if (_audioBGM == null || audioClip == null) return;

        // すでに同じBGMが流れていたら何もしない
        if (_audioBGM.clip == audioClip && _audioBGM.isPlaying) return;

        _audioBGM.Stop();
        _audioBGM.clip = audioClip;
        _audioBGM.volume = volumeBGM;
        _audioBGM.loop = true;
        _audioBGM.Play();
        _currentBGMClip = audioClip;
    }

    /// <summary>
    /// BGMを止める
    /// </summary>
    public void StopBGM()
    {
        if (_audioBGM == null) return;
        _audioBGM.Stop();
        _audioBGM.clip = null;
        _currentBGMClip = null;
    }

    /// <summary>
    /// シーンに応じてBGMを切り替える（必要な時だけ呼ぶ）
    /// </summary>
    public void BGMChange(SceneName nextScene)
    {
        // 同じシーンなら何もしない
        if (_currentBGMScene == nextScene) return;

        _currentBGMScene = nextScene;

        switch (nextScene)
        {
            case SceneName.Title:
                StopBGM();
                break;

            case SceneName.SelectScene:
                if (_audioClipsBGM.Count > 0)
                {
                    PlayBGM(_audioClipsBGM[0]);
                }
                break;

            case SceneName.VoiceGame:
                StopBGM();
                break;

            case SceneName.HideTasteGame:
                StopBGM();
                break;

            case SceneName.ImageWordGame:
                StopBGM();
                break;

            case SceneName.OverlapWordGame:
                StopBGM();
                break;

            default:
                StopBGM();
                break;
        }
    }

    // =========================
    // SE再生
    // =========================

    /// <summary>
    /// 単発SE再生
    /// </summary>
    public void PlaySE(AudioClip audioClip)
    {
        if (_audioSE.Count == 0 || _audioSE[0] == null || audioClip == null) return;

        // スライダーの値を AudioSource に反映
        _audioSE[0].volume = volumeSE;

        // volumeScale は指定せず、AudioSource.volume を使う
        _audioSE[0].PlayOneShot(audioClip);
    }

    /// <summary>
    /// 複数のSEを同時再生したい場合
    /// </summary>
    public void SomePlaySE(List<AudioClip> audioClips)
    {
        int count = 1;
        foreach (var a in audioClips)
        {
            if (count >= _audioSE.Count) break;

            var se = _audioSE[count];
            if (se != null && a != null)
            {
                se.volume = volumeSE;
                se.PlayOneShot(a);
            }
            count++;
        }
    }
}
