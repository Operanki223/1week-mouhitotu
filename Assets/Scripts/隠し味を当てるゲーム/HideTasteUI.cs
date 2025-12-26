using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DishSpritePair
{
    public string dishName;   // JSONの dish と対応（例："カレー"）
    public Sprite sprite;     // 対応する画像
}

public class HideTasteUI : MonoBehaviour
{
    [Header("ゲーム管理（JSON読み込み済み）")]
    [SerializeField] private HideTasteGameManager gameManager;

    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI dishText;          // 料理名
    [SerializeField] private Transform hintParent;              // ヒント親(VerticalLayout推奨)
    [SerializeField] private TextMeshProUGUI hintTextPrefab;    // ヒント用Textプレハブ

    [SerializeField] private Transform answerButtonParent;      // 回答ボタン親
    [SerializeField] private Button answerButtonPrefab;         // 回答ボタンプレハブ

    [SerializeField] private TextMeshProUGUI resultText;        // 正解/不正解メッセージ
    [SerializeField] private TextMeshProUGUI correctCountText;  // 正解数表示
    [SerializeField] private TextMeshProUGUI timerText;         // タイマー表示

    [Header("カウントダウン表示")]
    [SerializeField] private TextMeshProUGUI countdownText;     // 3・2・1・スタート！用
    [SerializeField] private int countdownSEIndex = 5;          // 3・2・1 に使うSEのインデックス
    [SerializeField] private int startSEIndex = 5;              // スタート！に使うSEのインデックス

    [Header("料理画像")]
    [SerializeField] private Image dishImage;
    [SerializeField] private List<DishSpritePair> dishSprites;

    [Header("ゲーム設定")]
    [SerializeField] private float hintInterval = 5f;           // ヒント間隔(秒)
    [SerializeField] private int optionCount = 10;              // 選択肢の数
    [SerializeField] private int mistakeLimit = 3;              // ミス上限回数
    [SerializeField] private float timeLimitPerQuestion = 20f;  // 1問の制限時間(秒)

    [Header("ゲームオーバー表示")]
    [SerializeField] private GameObject gameOverPanel;          // GameOver用パネル
    [SerializeField] private TextMeshProUGUI gameOverScoreText; // 最終スコア表示用(任意)

    // 内部状態
    private int currentIndex = 0;
    private string currentCorrectAnswer;
    private Coroutine hintCoroutine;
    private Coroutine timerCoroutine;
    private bool isQuestionActive = false;

    private int currentMistakes = 0;
    private int correctCount = 0;

    private Dictionary<string, Sprite> dishSpriteDict = new Dictionary<string, Sprite>();

    private void Awake()
    {
        // 料理名→Spriteの辞書を作っておく
        dishSpriteDict.Clear();
        foreach (var pair in dishSprites)
        {
            if (pair != null && !string.IsNullOrEmpty(pair.dishName) && pair.sprite != null)
            {
                if (!dishSpriteDict.ContainsKey(pair.dishName))
                {
                    dishSpriteDict.Add(pair.dishName, pair.sprite);
                }
            }
        }

        // GameOverパネルは最初は消しておく
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // カウントダウンテキストも最初は非表示にしておく
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = GetComponent<HideTasteGameManager>();
        }

        if (gameManager == null)
        {
            Debug.LogError("HideTasteGameManager が見つかりません。Inspector でアサインしてください。");
            return;
        }

        if (gameManager.tasteList == null || gameManager.tasteList.Count == 0)
        {
            Debug.LogError("tasteList が空です。JSONの読み込みを確認してください。");
            return;
        }

        UpdateCorrectCountText();

        // ★ 3秒カウントダウンしてからゲーム開始
        StartCoroutine(GameStartFlow());
    }

    /// <summary>
    /// シーン開始・リトライ時の「カウントダウン→ゲーム開始」の流れ
    /// </summary>
    private IEnumerator GameStartFlow()
    {
        // 一旦状態リセット
        ClearHintsAndButtons();
        isQuestionActive = false;

        if (resultText != null) resultText.text = "";
        if (timerText != null) timerText.text = "";

        // 3・2・1・スタート！のカウントダウン
        yield return StartCoroutine(CountdownCoroutine());

        // カウントダウン終了後に最初の問題開始
        StartNewQuestion();
    }

    /// <summary>
    /// 3・2・1・スタート！ のカウントダウン
    /// </summary>
    private IEnumerator CountdownCoroutine()
    {
        if (countdownText == null) yield break;

        countdownText.gameObject.SetActive(true);

        int count = 3;
        while (count > 0)
        {
            countdownText.text = count.ToString();

            // 3・2・1 のときにSEを鳴らす
            if (SoundManager.instance != null &&
                SoundManager.instance._audioClipsSE != null &&
                countdownSEIndex >= 0 &&
                countdownSEIndex < SoundManager.instance._audioClipsSE.Count)
            {
                SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[countdownSEIndex]);
            }

            yield return new WaitForSeconds(1f);
            count--;
        }

        // 「スタート！」表示
        countdownText.text = "スタート！";

        // スタート時のSE
        if (SoundManager.instance != null &&
            SoundManager.instance._audioClipsSE != null &&
            startSEIndex >= 0 &&
            startSEIndex < SoundManager.instance._audioClipsSE.Count)
        {
            SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[startSEIndex]);
        }

        yield return new WaitForSeconds(0.8f);

        countdownText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 新しい問題を開始
    /// </summary>
    private void StartNewQuestion()
    {
        // すでにGameOverなら何もしない
        if (gameOverPanel != null && gameOverPanel.activeSelf) return;

        ClearHintsAndButtons();
        isQuestionActive = true;

        if (resultText != null)
        {
            resultText.text = "";
        }

        // ランダムで問題選択（順番に回したいならcurrentIndex++管理に変えてOK）
        currentIndex = Random.Range(0, gameManager.tasteList.Count);
        HiddenTasteData q = gameManager.tasteList[currentIndex];

        // 料理名
        if (dishText != null)
        {
            dishText.text = $"料理：{q.dish}";
        }

        // 料理画像
        UpdateDishImage(q.dish);

        // 正解を保持
        currentCorrectAnswer = q.answer;

        // 回答ボタン生成
        GenerateAnswerButtons(q);

        // ヒント表示開始
        hintCoroutine = StartCoroutine(HintRoutine(q));

        // タイマー開始
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    /// <summary>
    /// 料理名に応じて画像変更
    /// </summary>
    private void UpdateDishImage(string dishName)
    {
        if (dishImage == null) return;

        if (dishSpriteDict.TryGetValue(dishName, out var sprite))
        {
            dishImage.sprite = sprite;
            dishImage.enabled = true;
        }
        else
        {
            dishImage.enabled = false;
        }
    }

    /// <summary>
    /// ヒントを5秒ごとに1つずつ生成して表示
    /// </summary>
    private IEnumerator HintRoutine(HiddenTasteData q)
    {
        if (q.hints == null || q.hints.Length == 0) yield break;

        for (int i = 0; i < q.hints.Length; i++)
        {
            if (i == 0)
            {
                // 1つ目は即時
            }
            else
            {
                yield return new WaitForSeconds(hintInterval);
            }

            if (!isQuestionActive) yield break;

            if (hintTextPrefab != null && hintParent != null)
            {
                var hintObj = Instantiate(hintTextPrefab, hintParent);
                hintObj.text = $"ヒント{i + 1}：{q.hints[i]}";
            }
        }
    }

    /// <summary>
    /// 制限時間カウントダウン
    /// </summary>
    private IEnumerator TimerRoutine()
    {
        float t = timeLimitPerQuestion;

        while (t > 0f && isQuestionActive)
        {
            t -= Time.deltaTime;

            if (timerText != null)
            {
                timerText.text = $"残り時間：{Mathf.CeilToInt(t)}秒";
            }

            yield return null;
        }

        if (!isQuestionActive) yield break;

        // 時間切れ扱い
        if (timerText != null)
        {
            timerText.text = "時間切れ！";
        }

        HandleMistake(isTimeOver: true);
    }

    /// <summary>
    /// 回答ボタン生成（10個）
    /// </summary>
    private void GenerateAnswerButtons(HiddenTasteData currentQuestion)
    {
        if (answerButtonPrefab == null || answerButtonParent == null) return;

        List<string> options = new List<string>();
        options.Add(currentQuestion.answer);

        int maxOption = Mathf.Min(optionCount, gameManager.tasteList.Count);

        while (options.Count < maxOption)
        {
            HiddenTasteData randomData = gameManager.tasteList[Random.Range(0, gameManager.tasteList.Count)];
            string candidate = randomData.answer;

            if (candidate == currentQuestion.answer) continue;
            if (options.Contains(candidate)) continue;

            options.Add(candidate);
        }

        // シャッフル
        for (int i = 0; i < options.Count; i++)
        {
            int r = Random.Range(i, options.Count);
            (options[i], options[r]) = (options[r], options[i]);
        }

        // ボタン生成
        foreach (string option in options)
        {
            Button btn = Instantiate(answerButtonPrefab, answerButtonParent);
            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = option;
            }

            string captured = option;
            btn.onClick.AddListener(() => OnAnswerButtonClicked(captured, btn));
        }
    }

    /// <summary>
    /// 回答ボタンが押されたとき
    /// </summary>
    private void OnAnswerButtonClicked(string selectedAnswer, Button button)
    {
        if (!isQuestionActive) return;

        if (selectedAnswer == currentCorrectAnswer)
        {
            // 正解
            isQuestionActive = false;
            SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[0]);

            correctCount++;
            UpdateCorrectCountText();

            if (resultText != null)
            {
                resultText.text = "<color=red>正解!</color>";
            }

            // 次の問題へ
            StartCoroutine(NextQuestionAfterDelay(1.0f));
        }
        else
        {
            // 不正解：このボタンは押せなくする
            if (button != null)
            {
                button.interactable = false;
            }

            HandleMistake(isTimeOver: false);
        }
    }

    /// <summary>
    /// ミスしたとき（不正解 or 時間切れ）
    /// </summary>
    private void HandleMistake(bool isTimeOver)
    {
        SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[1]);

        currentMistakes++;

        if (resultText != null)
        {
            if (isTimeOver)
            {
                resultText.text = $"時間切れ… 残りミス可能回数：{mistakeLimit - currentMistakes}";
            }
            else
            {
                resultText.text = $"<color=blue>不正解…</color> 残りミス可能回数：{mistakeLimit - currentMistakes}";
            }
        }

        // 3回ミスでゲームオーバー
        if (currentMistakes >= mistakeLimit)
        {
            GameOver();
        }
        else
        {
            // 問題はそのまま続行（別の選択肢は押せる）
            // もし「間違えたら即次の問題」にしたいならここで StartNewQuestion() を呼ぶ
        }
    }

    /// <summary>
    /// 正解数表示更新
    /// </summary>
    private void UpdateCorrectCountText()
    {
        if (correctCountText != null)
        {
            correctCountText.text = $"正解数：{correctCount}";
        }
    }

    /// <summary>
    /// 一定時間後に次の問題へ
    /// </summary>
    private IEnumerator NextQuestionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartNewQuestion();
    }

    /// <summary>
    /// ヒントとボタンを全削除＆コルーチン停止
    /// </summary>
    private void ClearHintsAndButtons()
    {
        if (hintCoroutine != null)
        {
            StopCoroutine(hintCoroutine);
            hintCoroutine = null;
        }

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        if (hintParent != null)
        {
            for (int i = hintParent.childCount - 1; i >= 0; i--)
            {
                Destroy(hintParent.GetChild(i).gameObject);
            }
        }

        if (answerButtonParent != null)
        {
            for (int i = answerButtonParent.childCount - 1; i >= 0; i--)
            {
                Destroy(answerButtonParent.GetChild(i).gameObject);
            }
        }
    }

    /// <summary>
    /// ゲームオーバー処理
    /// </summary>
    private void GameOver()
    {
        isQuestionActive = false;
        ClearHintsAndButtons();

        if (timerText != null)
        {
            timerText.text = "ゲーム終了";
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = $"正解数：{correctCount}";
        }
    }

    /// <summary>
    /// GameOverパネルの「リトライ」ボタンから呼ぶ用
    /// </summary>
    public void OnRetryButton()
    {
        currentMistakes = 0;
        correctCount = 0;
        UpdateCorrectCountText();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // リトライ時もカウントダウンから再スタート
        StartCoroutine(GameStartFlow());
    }
}
