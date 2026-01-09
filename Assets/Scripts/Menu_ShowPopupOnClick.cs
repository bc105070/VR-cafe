using UnityEngine;

public class Menu_ShowPopupOnClick : MonoBehaviour
{
    // Reference to the pop-up canvas (Menu_PopupCanvas)
    [SerializeField] private GameObject popupCanvas;

    // Unity Message
    // Called once when the scene starts
    private void Start()
    {
        if (popupCanvas != null)
        {
            // For testing: show the pop-up when the scene starts
            // Later you can change this to "false" and only show it on click.
            popupCanvas.SetActive(true);
        }
    }

    // (Optional) Later we can add a public function here,
    // for example: public void ShowPopup() { popupCanvas.SetActive(true); }
}
