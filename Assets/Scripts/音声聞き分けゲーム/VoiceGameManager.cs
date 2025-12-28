using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

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
    [SerializeField] GameObject TimerTextObj;
    [SerializeField] TextMeshProUGUI heartText;
    [SerializeField] GameObject _gameOverPanel;
    [SerializeField] TextMeshProUGUI _scoreText;
    [SerializeField] GameObject _hidePanel;
    [SerializeField] List<TMP_InputField> _inputFields = new List<TMP_InputField>();
    [SerializeField] List<GameObject> _inputFieldsObj = new List<GameObject>();

    [Header("UI: 最高正解数表示")]
    [SerializeField] private TextMeshProUGUI bestScoreText;

    [Header("ScoreManager のスロット番号")]
    [SerializeField] private int scoreSlotIndex = 0; // ← VoiceGame 用のスロット番号(0)

    List<AudioClip> audioClips;

    int wordsLimit = 2;

    float limitTime = 3;
    int heartLimit = 1;
    int scoreCount;
    bool noloop = true;
    bool deside = false;
    bool onDeside = false;
    bool ans = false;
    bool isGameOver = false;
    // 結果表示中はPlayGameを止めるためのフラグ
    bool isResultTime = false;
    bool isReplaying = false;
    int lastLimitTimeInt = -1;

    void Start()
    {
        SoundManager.instance.BGMChange(SceneName.VoiceGame);
        Reset();

        // ゲーム開始時に「過去の最高正解数」を表示しておく
        UpdateBestScoreUI();
    }

    void Update()
    {
        PlayGame();
    }

    void Reset()
    {
        heartText.text = "";
        _gameOverPanel.SetActive(false);
        _hidePanel.SetActive(false);
        scoreCount = 0;
        lastLimitTimeInt = -1;

        for (int i = 0; i < _inputFieldsObj.Count; i++)
        {
            if (i < 2)
                _inputFieldsObj[i].SetActive(true);
            else
                _inputFieldsObj[i].SetActive(false);
        }
    }

    public void ReStart()
    {
        ScenesManager.instance.SceneLoader(SceneName.VoiceGame);
    }

    void PlayGame()
    {
        if (isGameOver) return;
        if (isResultTime) return;

        limitTime -= Time.deltaTime;
        int currentSec = Mathf.CeilToInt(limitTime);
        if (currentSec != lastLimitTimeInt)
        {
            if (currentSec == 3 || currentSec == 2 || currentSec == 1)
            {
                SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[5]);
            }
            lastLimitTimeInt = currentSec;
        }

        if (limitTime > 0)
        {
            _hidePanel.SetActive(true);
            TimerTextObj.SetActive(true);
            onDeside = false;
            TimerText.text = limitTime.ToString("F0");
        }

        if (limitTime < 1)
        {
            limitTime = 0;
            TimerTextObj.SetActive(false);
            onDeside = true;
            _hidePanel.SetActive(false);

            if (noloop)
            {
                noloop = false;  // 二重起動防止

                audioClips = new List<AudioClip>();
                List<string> audioNames = new List<string>();
                List<int> _rnds = new List<int>();
                _rnds.Clear();

                WordsLimitChange();

                while (_rnds.Count < wordsLimit)
                {
                    int r = Random.Range(0, _voices.Count);
                    if (!_rnds.Contains(r))
                        _rnds.Add(r);
                }

                for (int i = 0; i < _voices.Count; i++)
                {
                    foreach (int n in _rnds)
                    {
                        if (n.Equals(i))
                        {
                            audioClips.Add(_voices[i]._audioClip);
                            audioNames.Add(_voices[i]._audioName);
                            Debug.Log(_voices[i]._audioName);
                        }
                    }
                }

                SoundManager.instance.SomePlaySE(audioClips);

                // ここで「待機してから判定する」非同期処理をスタート
                WaitInputAndCheck(audioNames).Forget();
            }
        }
    }

    public void RePlay()
    {
        if (isGameOver) return;
        if (audioClips == null || audioClips.Count == 0) return;
        if (!onDeside) return;
        if (isReplaying) return;

        RePlayRoutine().Forget();
    }

    private async UniTaskVoid RePlayRoutine()
    {
        isReplaying = true;
        isResultTime = true;

        int count = 3;

        while (count > 0)
        {
            TimerTextObj.SetActive(true);
            _hidePanel.SetActive(true);
            TimerText.text = count.ToString();
            SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[5]);
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            count--;
        }

        SoundManager.instance.SomePlaySE(audioClips);

        TimerTextObj.SetActive(true);
        TimerText.text = limitTime.ToString("F0");

        isResultTime = false;
        isReplaying = false;
        _hidePanel.SetActive(false);
    }

    // 入力を待ってから判定する非同期処理
    private async UniTaskVoid WaitInputAndCheck(List<string> strings)
    {
        await InputWait();
        await WordCheck(strings);  // ← ここは await にしておくと安全
    }

    private async UniTask InputWait()
    {
        await UniTask.WaitUntil(() => deside);
    }

    private async UniTask TrueText()
    {
        float _waitTime = 2f;
        await UniTask.Delay(TimeSpan.FromSeconds(_waitTime));
    }

    private void UpdateBestScoreUI()
    {
        if (bestScoreText == null)
        {
            Debug.LogWarning("[VoiceGame] bestScoreText が設定されていません");
            return;
        }
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[VoiceGame] ScoreManager.Instance が null です");
            return;
        }

        int best = ScoreManager.Instance.GetBestScore(scoreSlotIndex);
        Debug.Log($"[VoiceGame] UpdateBestScoreUI slot={scoreSlotIndex}, best={best}");
        bestScoreText.text = $"最高正解数：{best}回";
    }

    async Task WordCheck(List<string> strings)
    {
        int check_count = 0;

        foreach (string s in strings)
        {
            for (int i = 0; i < wordsLimit; i++)
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

            isResultTime = true;

            TimerTextObj.SetActive(true);
            TimerText.text = "正解";
            SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[0]);

            await TrueText();
        }
        else
        {
            Debug.Log("不正解");
            SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[1]);
            heartLimit--;
        }

        if (heartLimit < 1)
        {
            Debug.Log("ゲームオーバー");

            isGameOver = true;
            isResultTime = true;

            _gameOverPanel.SetActive(true);
            _scoreText.text = $"正解数 {scoreCount}回";

            int slot = scoreSlotIndex;

            // 今回のスコアをスロットにセット
            ScoreManager.Instance.SetScore(slot, scoreCount);

            // ベスト更新
            ScoreManager.Instance.SaveBestScore(slot);

            // デバッグログで中身確認
            int bestAfterSave = ScoreManager.Instance.GetBestScore(slot);
            Debug.Log($"[VoiceGame] GameOver score={scoreCount}, bestAfterSave={bestAfterSave}");

            // UI更新
            UpdateBestScoreUI();

            // Unityroom に合計ベストを送信
            ScoreManager.Instance.SendTotalBestScoreToUnityroom();
            return;
        }
        else
        {
            deside = false;
            limitTime = 3;
            noloop = true;
            lastLimitTimeInt = -1;
        }

        for (int i = 0; i < _inputFields.Count; i++)
        {
            _inputFields[i].text = "";
        }

        isResultTime = false;
    }

    public void DesideButton()
    {
        if (onDeside)
        {
            deside = true;
            Debug.Log("入力しました");
        }
    }

    void WordsLimitChange()
    {
        switch (scoreCount)
        {
            case 3:
                _inputFieldsObj[2].SetActive(true);
                wordsLimit = 3;
                break;
            case 7:
                _inputFieldsObj[3].SetActive(true);
                wordsLimit = 4;
                break;
            case 12:
                _inputFieldsObj[4].SetActive(true);
                wordsLimit = 5;
                break;
            case 18:
                _inputFieldsObj[5].SetActive(true);
                wordsLimit = 6;
                break;
            case 23:
                _inputFieldsObj[6].SetActive(true);
                wordsLimit = 7;
                break;
            case 30:
                _inputFieldsObj[7].SetActive(true);
                wordsLimit = 8;
                break;
            default:
                break;
        }
    }
}
