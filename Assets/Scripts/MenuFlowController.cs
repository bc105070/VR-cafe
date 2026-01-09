using UnityEngine;

public class MenuFlowController : MonoBehaviour
{
    [SerializeField] private GameObject menuRead;
    [SerializeField] private GameObject menuOrder;

    private void Awake()
    {
        ResetMenu();
    }

    // Resets the menu to the initial state (Read visible, Order hidden)
    public void ResetMenu()
    {
        SetMenuState(true, false);
    }

    // Shows Order and hides Read when clicked "Order Now!" button
    public void ShowOrderMenu()
    {
        SetMenuState(false, true);
    }

    // Helper method to handle panel visibility
    private void SetMenuState(bool showRead, bool showOrder)
    {
        if (menuRead != null) menuRead.SetActive(showRead);
        if (menuOrder != null) menuOrder.SetActive(showOrder);
    }

    // Completely hides the menu UI
    public void HideAll()
    {
        SetMenuState(false, false);
    }
}

 
