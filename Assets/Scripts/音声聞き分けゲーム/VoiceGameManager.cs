using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Voice
{
    [SerializeField] public AudioClip _audioClip;
    [SerializeField] public string _audioName;
}

public class VoiceGameManager : MonoBehaviour
{
    [SerializeField] List<Voice> _voices = new List<Voice>();
    [SerializeField] TextMeshProUGUI TimerText;
    [SerializeField] TextMeshProUGUI heartText;
    [SerializeField] GameObject _gameOverPanel;
    [SerializeField] TextMeshProUGUI _scoreText;
    [SerializeField] GameObject _hidePanel;
    [SerializeField] List<TMP_InputField> _inputFields = new List<TMP_InputField>();

    float limitTime = 3;
    int heartLimit = 3;
    int scoreCount;
    bool noloop = true;
    bool deside = false;
    bool onDeside = false;
    bool ans = false;

    void Start()
    {
        heartText.text = $"LIFE:{heartLimit}";
        _gameOverPanel.SetActive(false);
        _hidePanel.SetActive(false);
        scoreCount = 0;
    }

    void Update()
    {
        PlayGame();
    }

    void PlayGame()
    {
        limitTime -= Time.deltaTime;
        if (limitTime > 0)
        {
            _hidePanel.SetActive(true);
            onDeside = false;
            TimerText.text = limitTime.ToString("F0");
        }

        if (limitTime < 1)
        {
            limitTime = 0;
            TimerText.text = "";
            onDeside = true;
            _hidePanel.SetActive(false);

            if (noloop)
            {
                noloop = false;  // 二重起動防止を先にしておく

                List<AudioClip> audioClips = new List<AudioClip>();
                List<string> audioNames = new List<string>();
                foreach (var v in _voices)
                {
                    audioClips.Add(v._audioClip);
                    audioNames.Add(v._audioName);
                }

                SoundManager.instance.SomePlaySE(audioClips);

                // ここで「待機してから判定する」非同期処理をスタート
                WaitInputAndCheck(audioNames).Forget();
            }
        }
    }

    // 入力を待ってから判定する非同期処理
    private async UniTaskVoid WaitInputAndCheck(List<string> strings)
    {
        // 決定ボタンが押されるまで待つ
        await InputWait();

        // ここに来るのは deside == true になった後
        WordCheck(strings);
    }

    private async UniTask InputWait()
    {
        // deside が true になるまで待機
        await UniTask.WaitUntil(() => deside);
    }

    void WordCheck(List<string> strings)
    {
        int check_count = 0;

        foreach (string s in strings)
        {
            for (int i = 0; i < _inputFields.Count; i++)
            {
                if (_inputFields[i].text == s)
                {
                    check_count++;
                }
            }
        }

        ans = (check_count == strings.Count);

        if (ans)
        {
            Debug.Log("正解");
            scoreCount++;
        }
        else
        {
            Debug.Log("不正解");
            heartText.text = $"LIFE:{--heartLimit}";
        }

        if (heartLimit < 1)
        {
            Debug.Log("ゲームオーバー");
            _gameOverPanel.SetActive(true);
            _scoreText.text = $"正解数 {scoreCount}回";
        }
        else
        {
            // 次の問題に行くならここでリセットとか
            deside = false;
            limitTime = 3;
            noloop = true;
        }
    }

    public void DesideButton()
    {
        if (onDeside)
        {
            deside = true;
            Debug.Log("入力しました");
        }
    }
}
