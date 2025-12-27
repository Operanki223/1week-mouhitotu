using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#region JSON データクラス
[System.Serializable]
public class QuestionData
{
    public string resultImage;        // JSON側はこの名前のまま使う（画像の名前）
    public List<string> correctWords; // 正解の言葉（ふつうは2つ）
}

[System.Serializable]
public class QuestionDataList
{
    public List<QuestionData> questions;
}

[System.Serializable]
public class WordPoolData
{
    public List<string> wordPool;
}
#endregion

#region Sprite紐付け用クラス
[System.Serializable]
public class ResultImageEntry
{
    public string name;   // JSONの resultImage と一致させる（例: "カレー"）
    public Sprite sprite; // Inspectorで割り当てる画像
}
#endregion


public class ImageWordGameManager : MonoBehaviour
{
    [Header("JSON ファイル")]
    [SerializeField] private TextAsset _questionJson;
    [SerializeField] private TextAsset _wordPoolJson;

    [Header("画像紐付けリスト（Inspectorで設定）")]
    [SerializeField] private List<ResultImageEntry> _imageList = new List<ResultImageEntry>();
    private Dictionary<string, Sprite> _imageDict = new Dictionary<string, Sprite>();

    [Header("式 UI")]
    [SerializeField] private TextMeshProUGUI _firstWordText;
    [SerializeField] private TextMeshProUGUI _secondWordText;
    [SerializeField] private Image _resultImageUI;
    [SerializeField] private TextMeshProUGUI _imageNameText;   // 画像の上に出す名前

    [Header("判定 UI")]
    [SerializeField] private TextMeshProUGUI _judgeText;       // 正解・不正解を一瞬出す

    [Header("選択肢 UI")]
    [SerializeField] private List<Button> _choiceButtons = new List<Button>();
    [SerializeField] private List<TextMeshProUGUI> _choiceTexts = new List<TextMeshProUGUI>();

    [Header("ゲーム情報 UI")]
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _countdownText;   // 3・2・1 用

    [Header("リザルト UI")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private TextMeshProUGUI _resultScoreText;

    [Header("その他ボタン")]
    [SerializeField] private Button _rerollButton;             // 全ての選択肢を更新するボタン

    [Header("SE のインデックス（SoundManager._audioClipsSE 用）")]
    [SerializeField] private int _countdownSeIndex = 0;        // カウントダウン用
    [SerializeField] private int _clickSeIndex = 1;            // 選択肢ボタンクリック
    [SerializeField] private int _correctSeIndex = 2;          // 正解
    [SerializeField] private int _wrongSeIndex = 3;            // 不正解
    [SerializeField] private int _rerollSeIndex = 4;           // 選択肢更新ボタン

    [Header("設定")]
    [SerializeField] private float _timeLimit = 60f;           // ゲームの制限時間（秒）
    [SerializeField] private float _judgeDisplayTime = 0.6f;   // 正解/不正解表示時間

    private QuestionDataList _questionList;
    private WordPoolData _wordPoolData;

    private QuestionData _currentQuestion;
    private List<string> _currentWordsOnButtons = new List<string>();

    private string _firstSelectedWord = "";
    private string _secondSelectedWord = "";

    private int _score = 0;
    private float _timeRemaining;
    private bool _isGameOver = false;
    private bool _isTransition = false;
    private bool _isGameStarted = false; // カウントダウン終了後に true

    private Coroutine _judgeCoroutine;   // 正解・不正解表示用コルーチン


    private void Start()
    {
        SoundManager.instance.BGMChange(SceneName.ImageWordGame);
        // JSON読込
        _questionList = JsonUtility.FromJson<QuestionDataList>(_questionJson.text);
        _wordPoolData = JsonUtility.FromJson<WordPoolData>(_wordPoolJson.text);

        // 画像辞書化
        _imageDict.Clear();
        foreach (var e in _imageList)
        {
            if (!string.IsNullOrEmpty(e.name) && e.sprite != null && !_imageDict.ContainsKey(e.name))
            {
                _imageDict.Add(e.name, e.sprite);
            }
        }

        // リザルトパネルは最初非表示
        if (_resultPanel != null) _resultPanel.SetActive(false);

        // 判定テキスト初期化
        if (_judgeText != null)
        {
            _judgeText.text = "";
            _judgeText.gameObject.SetActive(false);
        }

        if (_countdownText != null) _countdownText.text = "";

        // スコア初期化
        _score = 0;
        UpdateScoreText();

        // ボタンイベント登録
        SetupButtonEvents();
        if (_rerollButton != null)
        {
            _rerollButton.onClick.AddListener(RerollAllChoices);
        }

        // 最初の問題をセット
        LoadRandomQuestion();
        SetupChoicesForCurrentQuestion();
        ResetSelectedWordsUI();

        // カウントダウン開始
        StartCoroutine(StartGameCountdown());
    }


    private void Update()
    {
        // ゲーム開始前 or 終了後はタイマーを減らさない
        if (!_isGameStarted || _isGameOver) return;

        _timeRemaining -= Time.deltaTime;
        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            _isGameOver = true;

            foreach (var btn in _choiceButtons)
            {
                btn.interactable = false;
            }
            if (_rerollButton != null) _rerollButton.interactable = false;

            // ゲーム終了 → リザルトパネル表示
            ShowResultPanel();
        }

        if (_timerText != null)
        {
            _timerText.text = $"TIME: {_timeRemaining:0.0}s";
        }
    }


    void UpdateScoreText()
    {
        if (_scoreText != null)
        {
            _scoreText.text = $"正解数: {_score}";
        }
    }

    void ResetSelectedWordsUI()
    {
        _firstSelectedWord = "";
        _secondSelectedWord = "";
        if (_firstWordText != null) _firstWordText.text = "？";
        if (_secondWordText != null) _secondWordText.text = "？";
    }


    // 3・2・1 カウントダウン
    private IEnumerator StartGameCountdown()
    {
        _isGameStarted = false;
        _isGameOver = false;

        // ボタンを押せないようにする
        foreach (var btn in _choiceButtons)
        {
            btn.interactable = false;
        }
        if (_rerollButton != null) _rerollButton.interactable = false;

        if (_countdownText != null)
        {
            _countdownText.gameObject.SetActive(true);

            _countdownText.text = "3";
            PlaySEByIndex(_countdownSeIndex);
            yield return new WaitForSeconds(1f);

            _countdownText.text = "2";
            PlaySEByIndex(_countdownSeIndex);
            yield return new WaitForSeconds(1f);

            _countdownText.text = "1";
            PlaySEByIndex(_countdownSeIndex);
            yield return new WaitForSeconds(1f);

            _countdownText.text = "スタート！";
            PlaySEByIndex(_countdownSeIndex);
            yield return new WaitForSeconds(0.7f);

            _countdownText.text = "";
            _countdownText.gameObject.SetActive(false);
        }

        // タイマーのカウント開始
        _timeRemaining = _timeLimit;
        _isGameStarted = true;

        // ボタンを押せるようにする
        foreach (var btn in _choiceButtons)
        {
            btn.interactable = true;
        }
        if (_rerollButton != null) _rerollButton.interactable = true;
    }


    // 画像＆名前の読み込み処理
    private void LoadRandomQuestion()
    {
        int idx = Random.Range(0, _questionList.questions.Count);
        _currentQuestion = _questionList.questions[idx];

        // 画像名（テキスト）を表示
        if (_imageNameText != null)
        {
            _imageNameText.text = _currentQuestion.resultImage;
        }

        // 画像Spriteを辞書から取得
        if (!string.IsNullOrEmpty(_currentQuestion.resultImage)
            && _imageDict.TryGetValue(_currentQuestion.resultImage, out Sprite sp))
        {
            if (_resultImageUI != null)
            {
                _resultImageUI.sprite = sp;
            }
        }
        else
        {
            Debug.LogWarning($"画像 '{_currentQuestion.resultImage}' がリストにありません！");
            if (_resultImageUI != null) _resultImageUI.sprite = null;
        }

        ResetSelectedWordsUI();
    }


    // 選択肢生成（6択）
    private void SetupChoicesForCurrentQuestion()
    {
        _currentWordsOnButtons.Clear();

        // まず正解を候補に入れる
        List<string> candidates = new List<string>(_currentQuestion.correctWords);

        // 不正解プール
        List<string> wrongPool = new List<string>(_wordPoolData.wordPool);
        wrongPool.RemoveAll(w => _currentQuestion.correctWords.Contains(w));
        Shuffle(wrongPool);

        // 6個になるまで不正解を詰める
        while (candidates.Count < _choiceButtons.Count && wrongPool.Count > 0)
        {
            candidates.Add(wrongPool[0]);
            wrongPool.RemoveAt(0);
        }

        // 念のため保険
        while (candidates.Count < _choiceButtons.Count)
        {
            candidates.Add("???");
        }

        Shuffle(candidates);

        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            _choiceTexts[i].text = candidates[i];
            _currentWordsOnButtons.Add(candidates[i]);
            _choiceButtons[i].interactable = true;
            _choiceButtons[i].image.color = Color.white;
        }
    }


    // すべての選択肢を更新するボタン用（式はリセットしない）
    private void RerollAllChoices()
    {
        if (!_isGameStarted) return;
        if (_isGameOver) return;
        if (_isTransition) return;

        PlaySEByIndex(_rerollSeIndex);

        // 画面上の候補だけ引き直す（式はそのまま）
        SetupChoicesForCurrentQuestion();
    }


    // クリック処理
    private void OnClickChoice(int index)
    {
        if (!_isGameStarted) return;     // カウントダウン中は無視
        if (_isGameOver) return;
        if (_isTransition) return;
        if (index < 0 || index >= _currentWordsOnButtons.Count) return;

        PlaySEByIndex(_clickSeIndex);

        string word = _currentWordsOnButtons[index];

        // まず式に入れる（正解でも不正解でも）
        // すでに2つ埋まっていたら一度クリアして最新2つだけ表示する
        if (!string.IsNullOrEmpty(_firstSelectedWord) &&
            !string.IsNullOrEmpty(_secondSelectedWord))
        {
            ResetSelectedWordsUI();
        }

        if (string.IsNullOrEmpty(_firstSelectedWord))
        {
            _firstSelectedWord = word;
            if (_firstWordText != null) _firstWordText.text = word;
        }
        else if (string.IsNullOrEmpty(_secondSelectedWord))
        {
            _secondSelectedWord = word;
            if (_secondWordText != null) _secondWordText.text = word;
        }

        // ★ 2つそろったら判定する
        if (!string.IsNullOrEmpty(_firstSelectedWord) &&
            !string.IsNullOrEmpty(_secondSelectedWord))
        {
            bool pairCorrect = IsCurrentPairCorrect();

            if (pairCorrect)
            {
                ShowJudge("正解！", new Color(1f, 1f, 0.3f)); // 黄色っぽい
                PlaySEByIndex(_correctSeIndex);

                _score++;
                UpdateScoreText();

                // 正解したので次の問題へ
                StartCoroutine(NextQuestionAfterDelay(0.5f));
            }
            else
            {
                ShowJudge("不正解", new Color(1f, 0.4f, 0.4f)); // 赤っぽい
                PlaySEByIndex(_wrongSeIndex);

                // ★ 不正解の場合は、表示して少し待ってから式をクリア
                StartCoroutine(ClearFormulaAfterDelay(_judgeDisplayTime));
            }
        }

        // 正解・不正解に関わらず、押したボタンの言葉を新しい候補に差し替える
        ReplaceWithNewWrongWord(index);
    }


    // 今の2語の組み合わせが正解かどうか
    private bool IsCurrentPairCorrect()
    {
        if (_currentQuestion == null || _currentQuestion.correctWords == null) return false;
        if (_currentQuestion.correctWords.Count < 2) return false;

        if (string.IsNullOrEmpty(_firstSelectedWord) || string.IsNullOrEmpty(_secondSelectedWord))
            return false;

        if (_firstSelectedWord == _secondSelectedWord)
            return false; // 同じ単語2回はNG

        // JSON側は「正解の2語」が入っている想定なので、
        // 2つとも correctWords に含まれていれば正解とする（順不同）
        return _currentQuestion.correctWords.Contains(_firstSelectedWord)
            && _currentQuestion.correctWords.Contains(_secondSelectedWord);
    }


    // 問題切り替え
    IEnumerator NextQuestionAfterDelay(float delay)
    {
        _isTransition = true;

        foreach (var btn in _choiceButtons)
        {
            btn.interactable = false;
        }
        if (_rerollButton != null) _rerollButton.interactable = false;

        yield return new WaitForSeconds(delay);

        LoadRandomQuestion();
        SetupChoicesForCurrentQuestion();

        foreach (var btn in _choiceButtons)
        {
            btn.interactable = true;
        }
        if (_rerollButton != null) _rerollButton.interactable = true;

        _isTransition = false;
    }


    /// <summary>
    /// 押されたボタンを、新しい「不正解ワード」に差し替える
    /// （画面にすでにあるもの・この問題の正解ワードは除外）
    /// </summary>
    private void ReplaceWithNewWrongWord(int index)
    {
        // 画面に既に表示されているワードを除外しつつ、新しいワードを取得
        HashSet<string> usedWords = new HashSet<string>(_currentWordsOnButtons);

        List<string> wrongPool = new List<string>(_wordPoolData.wordPool);
        // この問題の正解語は除外（必ずハズレ用）
        wrongPool.RemoveAll(w => _currentQuestion.correctWords.Contains(w));
        // 画面上にすでにあるものも除外
        wrongPool.RemoveAll(w => usedWords.Contains(w));

        if (wrongPool.Count == 0)
        {
            // 候補不足の保険
            _choiceTexts[index].text = "???";
            _currentWordsOnButtons[index] = "???";
            _choiceButtons[index].image.color = Color.red;
            return;
        }

        Shuffle(wrongPool);
        string newWord = wrongPool[0];

        _currentWordsOnButtons[index] = newWord;
        _choiceTexts[index].text = newWord;

        // 色はリセット
        _choiceButtons[index].image.color = Color.white;
    }


    void SetupButtonEvents()
    {
        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            int x = i;
            _choiceButtons[i].onClick.AddListener(() => OnClickChoice(x));
        }
    }


    void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    // リザルトパネルを開く
    private void ShowResultPanel()
    {
        if (_resultPanel != null)
        {
            _resultPanel.SetActive(true);
        }

        if (_resultScoreText != null)
        {
            _resultScoreText.text = $"正解数: {_score}";
        }
    }

    // SoundManager を使って SE 再生
    private void PlaySEByIndex(int index)
    {
        if (SoundManager.instance == null) return;
        if (SoundManager.instance._audioClipsSE == null) return;
        if (index < 0 || index >= SoundManager.instance._audioClipsSE.Count) return;

        var clip = SoundManager.instance._audioClipsSE[index];
        if (clip == null) return;

        SoundManager.instance.PlaySE(clip);
    }

    // 正解・不正解表示ヘルパー
    private void ShowJudge(string message, Color color)
    {
        if (_judgeText == null) return;

        if (_judgeCoroutine != null)
        {
            StopCoroutine(_judgeCoroutine);
        }
        _judgeCoroutine = StartCoroutine(JudgeRoutine(message, color));
    }

    private IEnumerator JudgeRoutine(string message, Color color)
    {
        _judgeText.gameObject.SetActive(true);
        _judgeText.text = message;
        _judgeText.color = color;

        yield return new WaitForSeconds(_judgeDisplayTime);

        _judgeText.text = "";
        _judgeText.gameObject.SetActive(false);
    }

    // ★ 不正解後に式をクリアする専用コルーチン
    private IEnumerator ClearFormulaAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetSelectedWordsUI();
    }
}
