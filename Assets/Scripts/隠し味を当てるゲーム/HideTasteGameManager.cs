using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HiddenTasteData
{
    public string dish;
    public string[] hints;
    public string answer;
    public string effect;
}

[System.Serializable]
public class HiddenTasteDataList
{
    public HiddenTasteData[] data;
}

public class HideTasteGameManager : MonoBehaviour
{
    public List<HiddenTasteData> tasteList = new List<HiddenTasteData>();

    // ここを Start ではなく Awake にする
    void Awake()
    {
        LoadJson();
    }

    // Start は空でOK（消してもOK）
    void Start()
    {
        SoundManager.instance.BGMChange(SceneName.HideTasteGame);
    }

    void LoadJson()
    {
        // Resourcesフォルダから読み込み（拡張子無し）
        TextAsset jsonFile = Resources.Load<TextAsset>("hiddenTaste");

        if (jsonFile == null)
        {
            Debug.LogError("JSONファイルが見つかりません。Resourcesに入ってる？");
            return;
        }

        // JSON → オブジェクト変換
        HiddenTasteDataList jsonData =
            JsonUtility.FromJson<HiddenTasteDataList>("{\"data\":" + jsonFile.text + "}");

        tasteList.Clear();               // 念のため初期化してから追加
        tasteList.AddRange(jsonData.data);

        Debug.Log("隠し味データ読み込み完了: " + tasteList.Count + "件");
    }

    public void ReStart()
    {
        ScenesManager.instance.SceneLoader(SceneName.HideTasteGame);
        Debug.Log("リスタート");
    }
}
