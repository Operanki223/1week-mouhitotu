using UnityEngine;

public class Buttons : MonoBehaviour
{
    public void PanelClose()
    {
        this.gameObject.SetActive(false);
        SoundManager.instance.PlaySE(SoundManager.instance._audioClips[3]);
    }

    public void PanelOpen()
    {
        this.gameObject.SetActive(true);
        SoundManager.instance.PlaySE(SoundManager.instance._audioClips[4]);
    }
}
