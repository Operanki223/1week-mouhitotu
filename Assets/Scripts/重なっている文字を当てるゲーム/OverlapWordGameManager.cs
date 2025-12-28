using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class WordDataList
{
    public string[] wordPool;
}

public class OverlapWordGameManager : MonoBehaviour
{
    [Header("重ねて表示する文字 UI（1文字ずつ）")]
    [SerializeField] private List<TextMeshProUGUI> _wordTexts = new List<TextMeshProUGUI>();
    [SerializeField] private List<GameObject> _wordTextObj = new List<GameObject>();
    [Range(2, 6)] public int _wordTextNum = 2;  // 重ねる文字の数 = 正解文字数

    [Header("選択肢 UI（ボタン）")]
    [SerializeField] private GameObject _ansPanel;
    [SerializeField] private List<Button> _choiceButtons = new List<Button>();
    [SerializeField] private List<TextMeshProUGUI> _choiceTexts = new List<TextMeshProUGUI>();

    [Header("ゲーム情報 UI")]
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _lastScoreText;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private TextMeshProUGUI _bestScoreText;

    [Header("文字をスライドさせるパネル")]
    [SerializeField] private RectTransform _wordPanel;

    [Header("答え合わせ時のスライド間隔")]
    [SerializeField] private float _slideSpacing = 200f; // 文字同士の距離。UIに合わせて調整


    [Header("カウントダウン UI")]
    [SerializeField] private TextMeshProUGUI _countdownText;

    [Header("ゲーム設定")]
    [SerializeField] private float _startTime = 60f;          // 初期持ち時間
    [SerializeField] private int _scorePerCorrect = 100;      // 正解1問あたりスコア
    [SerializeField] private float _timeBonusPerCorrect = 3f; // 正解時の時間ボーナス
    [SerializeField] private float _timePenaltyPerWrong = 0f; // 不正解時の時間減少（不要なら0）

    [Header("SE用インデックス（SoundManager._audioClipsSE の添字）")]
    [SerializeField] private int _seIndexCountdown = 0; // 3・2・1 のとき
    [SerializeField] private int _seIndexButton = 1;    // ボタン押したとき
    [SerializeField] private int _seIndexCorrect = 2;   // 正解
    [SerializeField] private int _seIndexWrong = 3;     // 不正解
    [SerializeField] private int _seIndexGameOver = 3;     // ゲームオーバー

    // ==== 内部データ ====
    private List<string> _wordStrings = new List<string>(); // JSON からの文字列
    private List<char> _charPool = new List<char>();        // 使用可能な文字一覧

    private List<char> _answerChars = new List<char>();     // 正解の文字たち
    private HashSet<char> _selectedChars = new HashSet<char>(); // プレイヤーが選んだ文字

    private float _remainTime;
    private int _score;
    private bool _isPlaying;      // タイマー動作中か
    private bool _acceptInput;    // 選択できる状態か（答え合わせ中は false）

    void Awake()
    {
        LoadJson();
        BuildCharPool();
    }

    void Start()
    {
        SoundManager.instance.BGMChange(SceneName.OverlapWordGame);
        // 問題テキストオブジェクトの有効/無効
        for (int i = 0; i < _wordTextObj.Count; i++)
        {
            _wordTextObj[i].SetActive(i < _wordTextNum);
        }

        // 念のためクリアしてから詰め直す
        _choiceButtons.Clear();
        _choiceTexts.Clear();

        // パネルの子オブジェクトから Button と TextMeshProUGUI を取得
        foreach (Transform child in _ansPanel.transform)
        {
            Button b = child.GetComponent<Button>();
            if (b != null)
            {
                _choiceButtons.Add(b);
            }

            TextMeshProUGUI t = child.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null)
            {
                t.text = "";
                _choiceTexts.Add(t);
            }
        }

        // ボタンクリック時の処理登録
        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            int idx = i;
            _choiceButtons[idx].onClick.AddListener(() =>
            {
                OnClickChoice(idx);
            });
        }

        _gameOverPanel?.SetActive(false);
        _countdownText?.gameObject.SetActive(false);

        StartCoroutine(GameStartRoutine());
    }

    void Update()
    {
        if (!_isPlaying) return;

        _remainTime -= Time.deltaTime;
        if (_remainTime < 0f) _remainTime = 0f;

        UpdateTimerUI();

        if (_remainTime <= 0f && _isPlaying)
        {
            _remainTime = 0f;
            UpdateTimerUI();
            GameOver();
        }
    }

    // ================== データ読み込み系 ==================

    void LoadJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("kanjiPool");

        if (jsonFile == null)
        {
            Debug.LogError("JSONファイルが見つかりません。Resources に kanjiPool.json がありますか？");
            return;
        }

        WordDataList jsonData = JsonUtility.FromJson<WordDataList>(jsonFile.text);

        _wordStrings.Clear();
        _wordStrings.AddRange(jsonData.wordPool);

        Debug.Log("文字データ読み込み完了: " + _wordStrings.Count + "件");
    }

    /// <summary>
    /// _wordStrings に含まれるすべての文字を集めて文字プールを作る
    /// </summary>
    void BuildCharPool()
    {
        HashSet<char> set = new HashSet<char>();

        foreach (var s in _wordStrings)
        {
            if (string.IsNullOrEmpty(s)) continue;

            foreach (var ch in s)
            {
                set.Add(ch);
            }
        }

        _charPool = new List<char>(set);
        Debug.Log("文字プール作成完了: " + _charPool.Count + "文字");

        if (_charPool.Count < _wordTextNum)
        {
            Debug.LogWarning("重ねる文字数より文字プールの数が少ないです");
        }
    }

    // ================== ゲーム開始・終了 ==================

    IEnumerator GameStartRoutine()
    {
        // 開始前は入力・タイマー停止
        _isPlaying = false;
        _acceptInput = false;
        _score = 0;
        _remainTime = _startTime;
        UpdateScoreUI();
        UpdateTimerUI();
        if (_resultText != null) _resultText.text = "";

        // カウントダウン表示
        if (_countdownText != null)
        {
            _countdownText.gameObject.SetActive(true);

            for (int i = 3; i >= 1; i--)
            {
                _countdownText.text = i.ToString();
                PlaySE(_seIndexCountdown);
                yield return new WaitForSeconds(1f);
            }

            _countdownText.text = "Start!";
            PlaySE(_seIndexCountdown);
            yield return new WaitForSeconds(0.8f);

            _countdownText.gameObject.SetActive(false);
        }

        // ゲーム開始
        _remainTime = _startTime;
        UpdateTimerUI();
        _isPlaying = true;
        _gameOverPanel?.SetActive(false);

        NextQuestion();
    }

    void GameOver()
    {
        PlaySE(_seIndexGameOver);
        SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[7]);

        int slot = 3; // このゲーム用のスロット番号

        // 今回のスコアをそのまま「スロット3のスコア」として登録
        ScoreManager.Instance.SetScore(slot, _score);

        // ベストスコア更新（前より大きいときだけ ScoreManager 内で更新される）
        ScoreManager.Instance.SaveBestScore(slot);

        int best = ScoreManager.Instance.GetBestScore(slot);

        _bestScoreText.text = $"最高スコア：{best}P";

        // （必要なら）Unityroom ランキングへ送信
        ScoreManager.Instance.SendTotalBestScoreToUnityroom();

        _isPlaying = false;
        _acceptInput = false;

        if (_resultText != null) _resultText.text = "タイムアップ！";

        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(true);
        }

        foreach (var btn in _choiceButtons)
        {
            btn.interactable = false;
        }
    }

    // ================== 問題生成 ==================

    void NextQuestion()
    {
        if (_charPool.Count == 0)
        {
            Debug.LogError("文字プールが空です");
            return;
        }

        // 正解の文字を決定
        List<char> poolCopy = new List<char>(_charPool);
        Shuffle(poolCopy);

        _answerChars.Clear();
        for (int i = 0; i < _wordTextNum && i < poolCopy.Count; i++)
        {
            _answerChars.Add(poolCopy[i]);
        }

        // ▼▼ ここが大事：全文字を親パネルの中央(0,0)に重ねる ▼▼
        for (int i = 0; i < _wordTexts.Count; i++)
        {
            if (i < _answerChars.Count)
            {
                _wordTextObj[i].SetActive(true);
                _wordTexts[i].text = _answerChars[i].ToString();

                RectTransform rt = _wordTexts[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    // 親を _wordPanel にしておくとより安全
                    if (_wordPanel != null && rt.parent != _wordPanel)
                    {
                        rt.SetParent(_wordPanel, worldPositionStays: false);
                    }

                    rt.anchoredPosition = Vector2.zero;
                    rt.localPosition = Vector3.zero;
                }

                // Z順を決める（重なり順）
                _wordTexts[i].transform.SetSiblingIndex(i);
            }
            else
            {
                _wordTextObj[i].SetActive(false);
                _wordTexts[i].text = "";
            }
        }
        // ▲▲ ここまでで「完全に重なった状態」を保証する ▲▲

        _selectedChars.Clear();
        ClearSelectionVisual();
        SetChoices();

        if (_resultText != null) _resultText.text = "";

        _acceptInput = true;
    }


    /// <summary>
    /// 選択肢の文字をセットする（正解 + ダミー）
    /// </summary>
    void SetChoices()
    {
        List<char> choiceChars = new List<char>(_answerChars);

        // ダミー候補（正解以外）
        List<char> dummyPool = new List<char>(_charPool);
        dummyPool.RemoveAll(c => _answerChars.Contains(c));
        Shuffle(dummyPool);

        int need = _choiceTexts.Count - choiceChars.Count;
        for (int i = 0; i < need && i < dummyPool.Count; i++)
        {
            choiceChars.Add(dummyPool[i]);
        }

        Shuffle(choiceChars);

        for (int i = 0; i < _choiceTexts.Count; i++)
        {
            if (i < choiceChars.Count)
            {
                _choiceTexts[i].text = choiceChars[i].ToString();
                _choiceButtons[i].interactable = true;
            }
            else
            {
                _choiceTexts[i].text = "";
                _choiceButtons[i].interactable = false;
            }
        }

        _selectedChars.Clear();
    }

    // ================== 入力処理 ==================

    void OnClickChoice(int index)
    {
        if (!_isPlaying) return;
        if (!_acceptInput) return;
        if (index < 0 || index >= _choiceTexts.Count) return;

        string txt = _choiceTexts[index].text;
        if (string.IsNullOrEmpty(txt)) return;

        // ボタン押下SE
        PlaySE(_seIndexButton);

        char c = txt[0];

        // すでに選択済み → 解除
        if (_selectedChars.Contains(c))
        {
            _selectedChars.Remove(c);
            SetButtonSelectedVisual(index, false);
        }
        else
        {
            // 正解文字数以上は選べない
            if (_selectedChars.Count >= _answerChars.Count)
            {
                return;
            }

            _selectedChars.Add(c);
            SetButtonSelectedVisual(index, true);
        }

        // 正解の数だけ選んだら答え合わせ
        if (_selectedChars.Count == _answerChars.Count)
        {
            _acceptInput = false;
            StartCoroutine(AnswerCheckRoutine());
        }
    }

    /// <summary>
    /// リセットボタン用（選択状態を元に戻す）
    /// </summary>
    public void OnClickReset()
    {
        if (!_isPlaying) return;
        if (!_acceptInput) return;

        _selectedChars.Clear();
        ClearSelectionVisual();
        if (_resultText != null) _resultText.text = "";
    }

    /// <summary>
    /// 正解かどうか判定 + 表示 + スライド演出 + 次の問題
    /// </summary>
    IEnumerator AnswerCheckRoutine()
    {
        // 集合として一致しているか？
        bool correct =
            _selectedChars.Count == _answerChars.Count &&
            !_answerChars.Except(_selectedChars).Any();

        if (correct)
        {
            _score += _scorePerCorrect;
            _remainTime += _timeBonusPerCorrect;
            UpdateScoreUI();
            UpdateTimerUI();

            if (_resultText != null) _resultText.text = "<color=red>正解</color>";
            PlaySE(_seIndexCorrect);
        }
        else
        {
            _remainTime -= _timePenaltyPerWrong;
            if (_remainTime < 0f) _remainTime = 0f;
            UpdateTimerUI();

            if (_resultText != null) _resultText.text = "<color=blue>不正解</color>";
            PlaySE(_seIndexWrong);
        }

        // ===== 答え合わせとして文字を横にスライド =====
        yield return StartCoroutine(SlideWordTexts());

        // ちょっとだけ結果を見せる時間
        yield return new WaitForSeconds(0.4f);

        // 選択状態リセット
        _selectedChars.Clear();
        ClearSelectionVisual();

        if (_isPlaying && _remainTime > 0f)
        {
            NextQuestion();
        }
    }

    /// <summary>
    /// 重なっていた文字が「指定パネルの中央」を基準に、左右に等間隔で広がる
    /// </summary>
    IEnumerator SlideWordTexts()
    {
        // 今回使っている文字の RectTransform を取得
        List<RectTransform> rts = new List<RectTransform>();
        for (int i = 0; i < _wordTexts.Count; i++)
        {
            if (i < _answerChars.Count)
            {
                var rt = _wordTexts[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    rts.Add(rt);
                }
            }
        }

        if (rts.Count == 0) yield break;

        // 基準となるパネル（設定があれば _wordPanel、なければ親）
        RectTransform panel = _wordPanel;
        if (panel == null)
        {
            panel = rts[0].transform.parent as RectTransform;
        }
        if (panel == null) yield break;

        // ★ ここでは panel.rect.width を使わず、
        //    「中央からの左右オフセット」だけで配置を決める
        float spacing = _slideSpacing; // インスペクタで調整可能
        int n = rts.Count;

        // スタート/ゴール位置
        List<Vector3> starts = new List<Vector3>();
        List<Vector3> targets = new List<Vector3>();

        for (int i = 0; i < n; i++)
        {
            RectTransform rt = rts[i];

            // スタートは完全に重なっている位置（localPosition で扱う）
            starts.Add(rt.localPosition);

            // 中央を0として、左右に等間隔に配置
            // 例: n=2 → [-s/2, +s/2]
            //     n=3 → [-s, 0, +s]
            float offsetIndex = i - (n - 1) / 2f;
            float x = offsetIndex * spacing;
            float y = 0f; // 縦は中央のまま

            targets.Add(new Vector3(x, y, rt.localPosition.z));
        }

        float duration = 0.4f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float rate = Mathf.SmoothStep(0f, 1f, t / duration);

            for (int i = 0; i < n; i++)
            {
                rts[i].localPosition = Vector3.Lerp(starts[i], targets[i], rate);
            }

            yield return null;
        }

        // 最終位置を固定
        for (int i = 0; i < n; i++)
        {
            rts[i].localPosition = targets[i];
        }
    }

    // ================== 見た目＆SE ==================

    void UpdateTimerUI()
    {
        if (_timerText == null) return;
        _timerText.text = $"残り時間：{Mathf.CeilToInt(_remainTime)}秒";
    }

    void UpdateScoreUI()
    {
        if (_scoreText == null) return;
        _scoreText.text = $"スコア: {_score}P";
    }

    void ClearSelectionVisual()
    {
        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            SetButtonSelectedVisual(i, false);
        }
    }

    void SetButtonSelectedVisual(int index, bool selected)
    {
        if (index < 0 || index >= _choiceButtons.Count) return;

        Image img = _choiceButtons[index].GetComponent<Image>();
        if (img == null) return;

        // 好きな色に変えてOK
        img.color = selected ? new Color(1f, 1f, 0.6f) : Color.white;
    }

    void PlaySE(int index)
    {
        if (SoundManager.instance == null) return;
        if (SoundManager.instance._audioClipsSE == null) return;
        if (index < 0 || index >= SoundManager.instance._audioClipsSE.Count) return;

        SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[index]);
    }

    // ================== 汎用：リストシャッフル ==================

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
}
