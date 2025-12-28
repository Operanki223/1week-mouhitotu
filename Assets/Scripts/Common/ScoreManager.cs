using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using Unityroom.Api; // unityroom公式APIパッケージを入れている場合
#endif

/// <summary>
/// 6つのスコアをまとめて管理＆セーブするマネージャー
/// ・どのシーンからでも ScoreManager.Instance でアクセス可能
/// ・PlayerPrefs に各スロットのベストスコアを保存
/// ・（任意）スロットごとに unityroom ランキングに送信可能
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // ====== シングルトン ======
    public static ScoreManager Instance { get; private set; }

    // スロット数（固定で6）
    public const int SlotCount = 6;

    [Header("現在のスコア（スロットごと）")]
    [SerializeField] private int[] _currentScores = new int[SlotCount];

    [Header("スコアID（保存用キー名の一部）")]
    [Tooltip("PlayerPrefsのキーを分けるためのIDです。必要に応じて名前を変えてOKです。")]
    [SerializeField]
    private string[] scoreIds = new string[SlotCount]
    {
        "Score0",//VoiceGame
        "Score1",//HideTasteGame
        "Score2",//ImageWordGame
        "Score3",//OverlapWrodGame
        "Score4",
        "Score5"
    };

    [Header("合計ベストスコア用 Unityroom ランキングID")]
    [SerializeField] private int totalBestRankingId = 0;

    [Header("Unityroom ランキングID（任意・使わないスロットは 0）")]
    [Tooltip("unityroom で作成したランキングのID（1,2,3...）。使わないスロットは 0 のままでOK")]
    [SerializeField]
    private int[] unityroomRankingIds = new int[SlotCount]
    {
        1, // Slot0用
        0, // Slot1用
        0,
        0,
        0,
        0
    };

    private void Awake()
    {
        // シングルトン化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // シーンをまたいでも残したい場合
    }

    // ===========================
    // スロットの範囲チェック
    // ===========================
    private bool IsValidSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount)
        {
            Debug.LogError($"[ScoreManager] slot は 0～{SlotCount - 1} の範囲で指定してください（指定値: {slot}）");
            return false;
        }
        return true;
    }

    // ===========================
    // 現在スコアの操作
    // ===========================

    /// <summary>
    /// スロットの現在スコアを取得
    /// </summary>
    public int GetCurrentScore(int slot)
    {
        if (!IsValidSlot(slot)) return 0;
        return _currentScores[slot];
    }

    /// <summary>
    /// スロットの現在スコアを0にリセット
    /// </summary>
    public void ResetCurrentScore(int slot)
    {
        if (!IsValidSlot(slot)) return;
        _currentScores[slot] = 0;
    }

    /// <summary>
    /// 全スロットの現在スコアを0にリセット
    /// </summary>
    public void ResetAllCurrentScores()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            _currentScores[i] = 0;
        }
    }

    /// <summary>
    /// スロットのスコアを加算
    /// </summary>
    public void AddScore(int slot, int value)
    {
        if (!IsValidSlot(slot)) return;
        _currentScores[slot] += value;
        if (_currentScores[slot] < 0) _currentScores[slot] = 0;
    }

    /// <summary>
    /// スロットのスコアを直接セット（残り時間＝スコアなど）
    /// </summary>
    public void SetScore(int slot, int value)
    {
        if (!IsValidSlot(slot)) return;
        _currentScores[slot] = Mathf.Max(0, value);
    }

    // ===========================
    // PlayerPrefs にベストスコアを保存
    // ===========================

    // PlayerPrefs のキーを組み立てる
    private string GetBestScoreKey(int slot)
    {
        if (!IsValidSlot(slot)) return "BestScore_Invalid";
        // 例: BestScore_Score0 など
        return $"BestScore_{scoreIds[slot]}";
    }

    /// <summary>
    /// スロットの現在スコアをベストスコアとして保存（高いときだけ更新）
    /// </summary>
    public void SaveBestScore(int slot)
    {
        if (!IsValidSlot(slot)) return;

        int score = _currentScores[slot];
        string key = GetBestScoreKey(slot);
        int best = PlayerPrefs.GetInt(key, 0);

        if (score > best)
        {
            PlayerPrefs.SetInt(key, score);
            PlayerPrefs.Save(); // WebGL では明示的に呼ぶ
            Debug.Log($"[ScoreManager] ベストスコア更新: slot={slot}, id={scoreIds[slot]}, score={score}");
        }
        else
        {
            Debug.Log($"[ScoreManager] ベストスコア更新無し: slot={slot}, current={score}, best={best}");
        }
    }

    /// <summary>
    /// スロットのベストスコアを取得
    /// </summary>
    public int GetBestScore(int slot)
    {
        if (!IsValidSlot(slot)) return 0;
        string key = GetBestScoreKey(slot);
        return PlayerPrefs.GetInt(key, 0);
    }

    /// <summary>
    /// スロットのベストスコアを削除
    /// </summary>
    public void ResetBestScore(int slot)
    {
        if (!IsValidSlot(slot)) return;
        string key = GetBestScoreKey(slot);
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        Debug.Log($"[ScoreManager] ベストスコアリセット: slot={slot}, id={scoreIds[slot]}");
    }

    /// <summary>
    /// 全スロットのベストスコアを削除
    /// </summary>
    public void ResetAllBestScores()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            ResetBestScore(i);
        }
    }

    // ===========================
    // unityroom ランキング送信（スロット単体）
    // ===========================

    /// <summary>
    /// 指定スロットの「現在スコア」を unityroom のランキングに送信
    /// unityroomRankingIds[slot] が 0 以下なら何もしない
    /// </summary>
    public void SendScoreToUnityroom(int slot)
    {
        if (!IsValidSlot(slot)) return;

        int rankId = unityroomRankingIds[slot];
        if (rankId <= 0)
        {
            Debug.LogWarning($"[ScoreManager] unityroomRankingIds[{slot}] が設定されていません。送信しません。");
            return;
        }

        int score = _currentScores[slot];

#if UNITY_WEBGL && !UNITY_EDITOR
        UnityroomApiClient.Instance.SendScore(rankId, score);
        Debug.Log($"[ScoreManager] unityroom ランキング(ID={rankId})にスロット{slot}のスコア {score} を送信しました。");
#else
        Debug.Log($"[ScoreManager] (エディタ) unityroomに送信する想定のスコア: {score} (slot={slot}, ID={rankId})");
#endif
    }

    // ===========================
    // 合計ベストスコア関連
    // ===========================

    /// <summary>
    /// 全スロットの「ベストスコア」の合計を取得
    /// </summary>
    public int GetTotalBestScore()
    {
        int total = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            total += GetBestScore(i);
        }
        return total;
    }

    /// <summary>
    /// 合計ベストスコアを unityroom のランキングに送信
    ///（totalBestRankingId に設定したランキングIDに送信）
    /// </summary>
    public void SendTotalBestScoreToUnityroom()
    {
        if (totalBestRankingId <= 0)
        {
            Debug.LogWarning("[ScoreManager] totalBestRankingId が設定されていません。合計ベストは送信しません。");
            return;
        }

        int totalBest = GetTotalBestScore();

#if UNITY_WEBGL && !UNITY_EDITOR
        UnityroomApiClient.Instance.SendScore(totalBestRankingId, totalBest);
        Debug.Log($"[ScoreManager] 合計ベストスコア {totalBest} を unityroom ランキング(ID={totalBestRankingId}) に送信しました。");
#else
        Debug.Log($"[ScoreManager] (エディタ) 合計ベストスコア {totalBest} を送信する想定 (ID={totalBestRankingId})");
#endif
    }
}
