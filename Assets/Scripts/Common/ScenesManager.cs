using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SceneData
{
    [SerializeField] private SceneAsset _sceneObject;
    [SerializeField] private SceneName _sceneName = SceneName.None;

    public SceneAsset GetSceneAsset() => this._sceneObject;
    public SceneName GetSceneName() => this._sceneName;
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
        }
    }

    public void SceneLoader(SceneName sceneName)
    {
        foreach (var s in _sceneDatas)
        {
            if (s.GetSceneName().Equals(sceneName))
            {
                SceneManager.LoadScene(s.GetSceneAsset().name);
            }
        }
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
}

public enum SceneName
{
    None,
    Title,
    SelectScene,
    VoiceGame,
    HideTasteGame,

}
