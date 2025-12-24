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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
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
    }
}

public enum SceneName
{
    None,
    Title,
    SelectScene,

}
