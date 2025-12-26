using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] AudioSource _audioBGM;
    [SerializeField] List<AudioSource> _audioSE = new List<AudioSource>();
    [SerializeField, Range(0.0f, 1.0f)] float volumeBGM = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)] float volumeSE = 0.5f;
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
        if (_audioBGM != null)
        {
            _audioBGM.loop = true;          // BGMはループ
            _audioBGM.volume = volumeBGM;   // 初期ボリューム
        }

        // SE側のボリュームも初期値を反映
        foreach (var se in _audioSE)
        {
            if (se != null) se.volume = volumeSE;
        }
    }

    void Update()
    {
        // 必要なら Inspector からリアルタイム音量調整したい時だけ反映
        if (_audioBGM != null)
        {
            _audioBGM.volume = volumeBGM;
        }

        foreach (var se in _audioSE)
        {
            if (se != null) se.volume = volumeSE;
        }

        // ★ ここではもう BGMChange(sceneName) は呼ばない ★
        //   シーンマネージャ側などから、シーン切り替えのタイミングで
        //   明示的に BGMChange(...) を呼ぶようにする
    }

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

            default:
                StopBGM();
                break;
        }
    }

    public void PlaySE(AudioClip audioClip)
    {
        if (_audioSE.Count == 0 || _audioSE[0] == null || audioClip == null) return;
        _audioSE[0].PlayOneShot(audioClip, volumeSE);
    }

    public void SomePlaySE(List<AudioClip> audioClips)
    {
        int count = 1;
        foreach (var a in audioClips)
        {
            if (count >= _audioSE.Count) break;
            if (_audioSE[count] != null && a != null)
            {
                _audioSE[count].PlayOneShot(a, volumeSE);
            }
            count++;
        }
    }
}
