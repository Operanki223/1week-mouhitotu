using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] AudioSource _audioBGM;
    [SerializeField] List<AudioSource> _audioSE = new List<AudioSource>();
    [SerializeField, Range(0.0f, 1.0f)] float volumeBGM = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)] float volumeSE = 0.5f;
    public static SoundManager instance;
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

    public void PlayBGM()
    {

    }

    public void PlayBGM(AudioClip audioClip)
    {
        _audioBGM.PlayOneShot(audioClip, volumeSE);
    }

    public void PlaySE(AudioClip audioClip)
    {
        _audioSE[0].PlayOneShot(audioClip, volumeSE);
    }

    public void SomePlaySE(List<AudioClip> audioClips)
    {
        int count = 1;
        foreach (var a in audioClips)
        {
            _audioSE[count].PlayOneShot(a, volumeSE);
            count++;
        }
    }
}
