using UnityEngine;

public class ConfigPanel : MonoBehaviour
{
    [SerializeField] GameObject _backHomeButton;
    [SerializeField] GameObject _restartButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (SoundManager.instance.sceneName == SceneName.SelectScene)
        {
            _backHomeButton.SetActive(false);
            _restartButton.SetActive(false);
        }
        else
        {
            _backHomeButton.SetActive(true);
            _restartButton.SetActive(true);
        }
    }

    public void ReStart()
    {
        Time.timeScale = 1;
        switch (SoundManager.instance.sceneName)
        {
            case SceneName.VoiceGame:
                ScenesManager.instance.VoiceGameScene();
                break;
            case SceneName.ImageWordGame:
                ScenesManager.instance.ImageWordGameScene();
                break;
            case SceneName.OverlapWordGame:
                ScenesManager.instance.OverlapWordGameScene();
                break;
            case SceneName.HideTasteGame:
                ScenesManager.instance.HideTasteGameScene();
                break;
            default:
                break;
        }

    }
}