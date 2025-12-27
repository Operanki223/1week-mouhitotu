using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SceneData
{
#if UNITY_EDITOR
    // エディタ上でシーンをドラッグ＆ドロップする用
    [SerializeField] private UnityEditor.SceneAsset _sceneObject;
#endif
    [SerializeField] private SceneName _sceneName = SceneName.None;

    // 実際にロードに使うシーン名（Build Settings に登録されている名前）
    [SerializeField] private string _scenePath;

#if UNITY_EDITOR
    public UnityEditor.SceneAsset GetSceneAsset() => _sceneObject;
#endif

    public SceneName GetSceneName() => _sceneName;
    public string GetScenePath() => _scenePath;

#if UNITY_EDITOR
    /// <summary>
    /// エディタで SceneAsset を変更したときに呼び出して、
    /// 自動的に _scenePath（シーン名）を更新する
    /// </summary>
    public void UpdateScenePathFromAsset()
    {
        if (_sceneObject == null)
        {
            _scenePath = string.Empty;
            return;
        }

        // シーンファイル名をそのままシーン名として使う
        // 例: "Assets/Scenes/Title.unity" → "Title"
        var scenePath = UnityEditor.AssetDatabase.GetAssetPath(_sceneObject);
        var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        _scenePath = sceneName;
    }
#endif
}

public class ScenesManager : MonoBehaviour
{
    public static ScenesManager instance;

    [SerializeField] private List<SceneData> _sceneDatas = new List<SceneData>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // シーンをまたいで使いたければコメントアウト解除
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// インスペクタで値が変わったときに呼ばれる
    /// SceneAsset から自動で _scenePath を更新する
    /// </summary>
    private void OnValidate()
    {
        if (_sceneDatas == null) return;

        foreach (var data in _sceneDatas)
        {
            data?.UpdateScenePathFromAsset();
        }
    }
#endif

    public void SceneLoader(SceneName sceneName)
    {
        foreach (var s in _sceneDatas)
        {
            if (s.GetSceneName() == sceneName)
            {
                var scenePath = s.GetScenePath();

                if (string.IsNullOrEmpty(scenePath))
                {
                    Debug.LogWarning($"SceneName {sceneName} のシーン名が設定されていません。");
                    return;
                }

                SceneManager.LoadScene(scenePath);
                return;
            }
        }

        Debug.LogWarning($"SceneName {sceneName} がシーンリストに見つかりません。");
    }

    public void TitleButton()
    {
        SceneLoader(SceneName.Title);
    }

    public void GameSelectSceneButton()
    {
        SceneLoader(SceneName.SelectScene);
        SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[2]);
    }

    public void VoiceGameScene()
    {
        SceneLoader(SceneName.VoiceGame);
        SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[5]);
    }

    public void HideTasteGameScene()
    {
        SceneLoader(SceneName.HideTasteGame);
        SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[5]);
    }

    public void ImageWordGameScene()
    {
        SceneLoader(SceneName.ImageWordGame);
        SoundManager.instance.PlaySE(SoundManager.instance._audioClipsSE[5]);
    }
}

public enum SceneName
{
    None,
    Title,
    SelectScene,
    VoiceGame,
    HideTasteGame,
    ImageWordGame,
}
