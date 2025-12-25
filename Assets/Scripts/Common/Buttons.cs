using UnityEngine;

public class Buttons : MonoBehaviour
{
    public void PanelClose()
    {
        this.gameObject.SetActive(false);
    }

    public void PanelOpen()
    {
        this.gameObject.SetActive(true);
    }
}
