using UnityEngine;

public class Menu_OptionHandler : MonoBehaviour
{
    // Reference to the pop-up canvas (the panel with the three options)
    [SerializeField] private GameObject popupCanvas;

    // This index is specific to THIS button (e.g., 1 for Option1, 2 for Option2, 3 for Option3)
    [SerializeField] private int optionIndex = -1;

    // Static value so other scripts can easily read the last selected option
    public static int LastSelectedOption { get; private set; } = -1;

    // Optional: if you also want this script to be able to close the popup at start
    // you can uncomment these lines and assign popupCanvas only on one object.
    //private void Start()
    //{
    //    if (popupCanvas != null)
    //    {
    //        popupCanvas.SetActive(false);
    //    }
    //}

    /// <summary>
    /// Called by the Button OnClick() event.
    /// This has NO parameters, so Unity will always show it in the dropdown.
    /// </summary>
    public void SelectThisOption()
    {
        // Save the selected option
        LastSelectedOption = optionIndex;

        // Print the result to the Unity Console
        Debug.Log($"[Menu] User selected option: {optionIndex}");

        // Close the pop-up window after selection
        if (popupCanvas != null)
        {
            popupCanvas.SetActive(false);
        }
    }
}
