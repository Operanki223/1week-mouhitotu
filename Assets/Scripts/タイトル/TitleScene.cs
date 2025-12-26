using UnityEngine;

public class TitleScene : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SoundManager.instance.BGMChange(SceneName.Title);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
