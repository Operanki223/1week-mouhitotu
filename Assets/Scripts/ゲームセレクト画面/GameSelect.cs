using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class GamePanel
{
    [SerializeField] private GameObject _scenePanel;
    [SerializeField] private SceneName _sceneName = SceneName.None;

    public GameObject GetScenePanel() => this._scenePanel;
    public SceneName GetSceneName() => this._sceneName;
}
public class GameSelect : MonoBehaviour
{
    [SerializeField] List<GamePanel> _gamePanels = new List<GamePanel>();

    void Start()
    {
        //全パネルの非表示
        foreach (var p in _gamePanels)
        {
            p.GetScenePanel().SetActive(false);
        }
    }

    public void GamePanelSelect(SceneName sceneName)
    {
        foreach (var p in _gamePanels)
        {
            if (p.GetSceneName().Equals(sceneName))
            {
                p.GetScenePanel().SetActive(true);
            }
        }
    }

    public void VoiceGamePanel()
    {
        GamePanelSelect(SceneName.VoiceGame);
    }
}
