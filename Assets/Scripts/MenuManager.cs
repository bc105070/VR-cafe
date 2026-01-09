using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("References")]
    public StateManagement stateManager;   // Drag your StateManagement here in the Inspector

    [Header("Menu 1: Read-only menu")]
    public GameObject menuReadRoot;        // Root of Menu_Read
    public Button orderNowButton;          // "Order Now!" button

    [Header("Menu 2: Order (five options, single choice)")]
    public GameObject menuOrderRoot;       // Root of Menu_Order (panel with the 5 options)
    public ToggleGroup foodToggleGroup;    // ToggleGroup that contains the 5 food toggles

    [Header("Menu 3: Confirm Yes / No")]
    public GameObject orderConfirmRoot;    // Root of Order_Confirm (Yes/No panel)
    public Toggle yesToggle;               // Menu_Order/Order_Confirm/Yes
    public Toggle noToggle;                // Menu_Order/Order_Confirm/No
    public ToggleGroup confirmToggleGroup; // The group for Yes/No toggles

    // Optional: name of the CSV file. 
    public string fallbackCsvFileName = "experiment_data.csv";

    private string selectedFoodId = null;

    private void Start()
    {
        // Register "Order Now!" button
        if (orderNowButton != null)
        {
            orderNowButton.onClick.AddListener(OnOrderNowClicked);
        }

        // Register listeners for each food option (five toggles)
        if (foodToggleGroup != null)
        {
            Toggle[] foodToggles = foodToggleGroup.GetComponentsInChildren<Toggle>();
            foreach (var toggle in foodToggles)
            {
                // Use local variable to avoid closure issue
                Toggle localToggle = toggle;
                localToggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        OnFoodOptionSelected(localToggle);
                    }
                });
            }
        }

        // Initial UI state
        if (menuOrderRoot != null) menuOrderRoot.SetActive(false);
        if (orderConfirmRoot != null) orderConfirmRoot.SetActive(false);

        // Register Yes/No listeners (no extra confirm button)
        if (yesToggle != null)
        {
            yesToggle.onValueChanged.AddListener(OnYesValueChanged);
        }
        if (noToggle != null)
        {
            noToggle.onValueChanged.AddListener(OnNoValueChanged);
        }
    }

    /// <summary>
    /// Step 4: "Order Now!" has been clicked on Menu_Read.
    /// </summary>
    public void OnOrderNowClicked()   // ★ 注意：public + 無參數，Button 才看得到
    {
        Debug.Log("[MenuManager] OrderNow clicked!");

        // Inform StateManagement (set IsOrderNowClicked + advance to Phase 2)
        if (stateManager != null)
        {
            stateManager.StartPhase2();
        }

        // Hide Menu 1
        if (menuReadRoot != null) menuReadRoot.SetActive(false);
        // Menu 2 will be shown by Agent/StateManagement when the agent arrives
        // if (menuOrderRoot != null) menuOrderRoot.SetActive(true);

        // Reset previous selection
        selectedFoodId = null;
        if (foodToggleGroup != null)
        {
            foreach (var t in foodToggleGroup.GetComponentsInChildren<Toggle>())
            {
                t.isOn = false;
            }
        }

        if (orderConfirmRoot != null) orderConfirmRoot.SetActive(false);
    }

    /// <summary>
    /// Step 6: customer chooses one of the five food options.
    /// </summary>
    private bool isInitializing = false;

    /// <summary>
    /// Step 6: customer chooses one of the five food options.
    /// </summary>
    private void OnFoodOptionSelected(Toggle toggle)
    {
        if (isInitializing) return; // Ignore events during setup

        // Use toggle name as the option ID (you can change this if you prefer explicit IDs)
        selectedFoodId = toggle.name;
        Debug.Log($"[MenuManager] Food selected: {selectedFoodId}");

        // Mark food selected in global state
        if (stateManager != null)
        {
            stateManager.isFoodSelected = true;
        }

        // Immediately go to Yes/No confirmation
        if (menuOrderRoot != null) menuOrderRoot.SetActive(false);
        if (orderConfirmRoot != null) orderConfirmRoot.SetActive(true);

        // Reset Yes/No without triggering events
        // Temporarily disable ToggleGroup to allow both to be off
        ToggleGroup tempGroup = null;
        if (yesToggle != null)
        {
            tempGroup = yesToggle.group;
            yesToggle.group = null;
            yesToggle.SetIsOnWithoutNotify(false);
        }
        if (noToggle != null)
        {
            noToggle.group = null;
            noToggle.SetIsOnWithoutNotify(false);
        }
        
        // Re-enable ToggleGroup
        if (tempGroup != null)
        {
            if (yesToggle != null) yesToggle.group = tempGroup;
            if (noToggle != null) noToggle.group = tempGroup;
        }
        
        // Re-register listeners to ensure they're bound (defensive programming)
        RegisterConfirmListeners();
    }
    
    private void RegisterConfirmListeners()
    {
        Debug.Log("[MenuManager] RegisterConfirmListeners called");
        Debug.Log($"[MenuManager] yesToggle = {(yesToggle == null ? "NULL" : yesToggle.name)}");
        Debug.Log($"[MenuManager] noToggle = {(noToggle == null ? "NULL" : noToggle.name)}");
        
        if (yesToggle != null)
        {
            yesToggle.onValueChanged.RemoveListener(OnYesValueChanged);
            yesToggle.onValueChanged.AddListener(OnYesValueChanged);
            Debug.Log($"[MenuManager] Yes listener registered. Listener count: {yesToggle.onValueChanged.GetPersistentEventCount()}");
        }
        else
        {
            Debug.LogError("[MenuManager] yesToggle is NULL! Cannot register listener.");
        }
        
        if (noToggle != null)
        {
            noToggle.onValueChanged.RemoveListener(OnNoValueChanged);
            noToggle.onValueChanged.AddListener(OnNoValueChanged);
            Debug.Log($"[MenuManager] No listener registered. Listener count: {noToggle.onValueChanged.GetPersistentEventCount()}");
        }
        else
        {
            Debug.LogError("[MenuManager] noToggle is NULL! Cannot register listener.");
        }
    }

    /// <summary>
    /// Prepare Phase 2 UI: ensure order panel visible, confirm hidden, reset selection state.
    /// Called from StateManagement when Phase 2 is shown programmatically.
    /// </summary>
    public void PreparePhase2UI()
    {
        StartCoroutine(PreparePhase2UICoroutine());
    }

    private System.Collections.IEnumerator PreparePhase2UICoroutine()
    {
        isInitializing = true;
        
        if (menuOrderRoot != null) menuOrderRoot.SetActive(true);
        if (orderConfirmRoot != null) orderConfirmRoot.SetActive(false);

        selectedFoodId = null;

        // Reset toggles without firing events
        if (foodToggleGroup != null)
        {
            // Pass 'true' to include inactive children, so we can reset them before showing
            foreach (var t in foodToggleGroup.GetComponentsInChildren<Toggle>(true))
            {
                t.SetIsOnWithoutNotify(false);
            }
            Debug.Log("[MenuManager] Reset all food toggles");
        }

        // Reset state flag
        if (stateManager != null)
        {
            stateManager.isFoodSelected = false;
        }

        // Wait for next frame to ensure UI is fully initialized before accepting events
        yield return null;
        
        isInitializing = false;
        Debug.Log("[MenuManager] PreparePhase2UI completed");
    }


    /// <summary>
    /// Yes selected → send order, write CSV, go to Phase 3 (survey).
    /// </summary>
    private void OnYesValueChanged(bool isOn)
    {
        Debug.Log($"[MenuManager] OnYesValueChanged called, isOn={isOn}");
        
        if (!isOn) return;  // only react when turned ON

        if (selectedFoodId == null)
        {
            Debug.LogWarning("[MenuManager] Yes clicked but no food selected.");
            return;
        }

        ExperimentSession s = ExperimentSession.Instance;
        if (s == null)
        {
            Debug.LogError("[MenuManager] ExperimentSession.Instance is null. Cannot write CSV.");
            return;
        }

        // Save order
        s.orderChoice = selectedFoodId;

        // Hide all menus
        if (menuReadRoot != null) menuReadRoot.SetActive(false);
        if (menuOrderRoot != null) menuOrderRoot.SetActive(false);
        if (orderConfirmRoot != null) orderConfirmRoot.SetActive(false);

        // Inform StateManagement: ordering confirmed → go to Phase 3
        if (stateManager != null)
        {
            stateManager.StartPhase3();
        }

        Debug.Log("[MenuManager] YES selected: order confirmed and CSV saved.");
    }

    /// <summary>
    /// No selected → go back to Menu 2 to choose again.
    /// </summary>
    private void OnNoValueChanged(bool isOn)
    {
        Debug.Log($"[MenuManager] OnNoValueChanged called, isOn={isOn}");
        
        if (!isOn) return;  // only react when turned ON

        Debug.Log("[MenuManager] NO clicked: returning to menu order");

        ExperimentSession s = ExperimentSession.Instance;
        if (s != null)
        {
            s.q1Choice = "No";
        }

        // Step 1: Hide confirm panel
        if (orderConfirmRoot != null) orderConfirmRoot.SetActive(false);
        
        // Step 2: Show menu panel
        if (menuOrderRoot != null) menuOrderRoot.SetActive(true);

        // Step 3: Reset selection state so user can re-select
        selectedFoodId = null;
        if (stateManager != null)
        {
            stateManager.isFoodSelected = false;
        }

        // Step 4: Reset all food toggles
        if (foodToggleGroup != null)
        {
            foreach (var t in foodToggleGroup.GetComponentsInChildren<Toggle>(true))
            {
                t.SetIsOnWithoutNotify(false);
            }
        }

        Debug.Log("[MenuManager] Menu order panel shown, ready for new selection");
    }

    private void Update()
    {
        // FAILSAFE: Ensure Menu 2 and Menu 3 never overlap
        if (menuOrderRoot != null && orderConfirmRoot != null)
        {
            if (menuOrderRoot.activeSelf && orderConfirmRoot.activeSelf)
            {
                Debug.LogWarning("[MenuManager] Overlap detected! Force hiding Confirm menu.");
                orderConfirmRoot.SetActive(false);
            }
        }
    }
}
