using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(CSVWriter))]
public class StateManagement : MonoBehaviour
{
    [Header("Configuration")]
    public string participantsCsvName = "Participants.csv";

    [Header("Audio References")]
    public AudioClip[] audioClips;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    [Tooltip("仅控制上方 Audio Clips 列表音轨的音量")]
    public float clipsVolume = 0.4f;

    [Header("Post Processing Volume")]
    public Volume postProcessingVolume;

    [Header("GameObjects to Toggle")]
    public GameObject menu;
    public GameObject food;
    public GameObject ordering;
    public GameObject survey;
    public GameObject thankYou;

    [Header("Managers")]
    public MenuManager menuManager;
    public SurveyManager surveyManager;
    
    [SerializeField, Tooltip("Auto-assigned if on same GameObject")]
    private CSVWriter _csvWriter;
    
    private CSVWriter csvWriter
    {
        get
        {
            if (_csvWriter == null)
            {
                _csvWriter = GetComponent<CSVWriter>();
            }
            return _csvWriter;
        }
    }
    
    [Header("External References")]
    public VoicePlayer waiterVoicePlayer;
    public AgentDestinationSetter agent;
    public AgentDestinationSetter agentDestinationSetter;
    public ConfirmationAudioPlayer confirmationAudioPlayer; // ✅ Simple reference to audio GameObject

    [Header("Status (Read Only in Inspector)")]
    public int participantID;
    public int condition;
    public int currentPhase;
    public string selectedFoodId = null;
    public int[] selectedOptions;
    public bool isOrderNowClicked;
    public bool isFoodSelected;
    public bool isOrderingConfirmed;
    public bool isSurveyCompleted;

    // Properties
    public bool IsOrderNowClicked { get => isOrderNowClicked; set => isOrderNowClicked = value; }
    public bool IsFoodSelected { get => isFoodSelected; set => isFoodSelected = value; }
    public bool IsOrderingConfirmed { get => isOrderingConfirmed; set => isOrderingConfirmed = value; }
    public bool IsSurveyCompleted { get => isSurveyCompleted; set => isSurveyCompleted = value; }

    private void Awake()
    {
        Debug.Log($"[StateManagement] ========== AWAKE ==========");
        Debug.Log($"[StateManagement] GameObject: '{gameObject.name}'");
        Debug.Log($"[StateManagement] InstanceID: {GetInstanceID()}");
        
        // Check for duplicate instances
        StateManagement[] allInstances = FindObjectsByType<StateManagement>(FindObjectsSortMode.None);
        if (allInstances.Length > 1)
        {
            Debug.LogWarning($"[StateManagement] ⚠️ Found {allInstances.Length} StateManagement instances! Should only have 1.");
            for (int i = 0; i < allInstances.Length; i++)
            {
                Debug.LogWarning($"  [{i}] {allInstances[i].gameObject.name} (InstanceID: {allInstances[i].GetInstanceID()})");
            }
        }
        
        // Initialize CSVWriter
        if (_csvWriter == null)
        {
            _csvWriter = GetComponent<CSVWriter>();
        }
        
        if (_csvWriter == null)
        {
            Debug.LogError("[StateManagement] CRITICAL: CSVWriter not found!");
        }
        else
        {
            Debug.Log($"[StateManagement] ✓ CSVWriter ready: {_csvWriter.gameObject.name}");
        }
        
        // Initialize ConfirmationAudioPlayer
        if (confirmationAudioPlayer == null)
        {
            Debug.LogWarning("[StateManagement] ConfirmationAudioPlayer not assigned in Inspector. Auto-finding...");
            confirmationAudioPlayer = FindAnyObjectByType<ConfirmationAudioPlayer>();
            
            if (confirmationAudioPlayer != null)
            {
                Debug.Log($"[StateManagement] ✓ Auto-found ConfirmationAudioPlayer on: {confirmationAudioPlayer.gameObject.name}");
            }
            else
            {
                Debug.LogError("[StateManagement] ✗ CRITICAL: No ConfirmationAudioPlayer found in scene! Create one with ConfirmationAudioPlayer component.");
            }
        }
        else
        {
            Debug.Log($"[StateManagement] ✓ ConfirmationAudioPlayer assigned: {confirmationAudioPlayer.gameObject.name}");
        }
        
        Debug.Log("[StateManagement] ========== AWAKE COMPLETE ==========");
    }

    private void Start()
    {
        Debug.Log("StateManagement is alive!");
        

        // Verify CSVWriter (use property for reading)
        if (csvWriter != null)
        {
            Debug.Log($"[StateManagement] ✓ CSVWriter confirmed: {csvWriter.gameObject.name}");
        }
        else
        {
            Debug.LogError("[StateManagement] ✗ CSVWriter is NULL in Start()!");
            
            // Try to auto-find (assign to backing field)
            _csvWriter = GetComponent<CSVWriter>();
            
            if (_csvWriter == null)
            {
                _csvWriter = FindAnyObjectByType<CSVWriter>();
            }
            
            if (csvWriter != null)
            {
                Debug.Log($"[StateManagement] ✓ Auto-found CSVWriter on: {csvWriter.gameObject.name}");
            }
            else
            {
                Debug.LogError("[StateManagement] ✗ No CSVWriter found in scene!");
            }
        }

        // Participant ID is stored in PlayerPrefs
        if (!PlayerPrefs.HasKey("ParticipantID"))
        {
            Debug.LogWarning("ParticipantID not found in PlayerPrefs. Using default value 1. " +
                             "Please set ParticipantID using your login/parameter scene.");
            participantID = 1;
            PlayerPrefs.SetInt("ParticipantID", participantID);
        }
        else
        {
            participantID = PlayerPrefs.GetInt("ParticipantID");
        }

        // Get condition from PlayerPrefs
        if (!PlayerPrefs.HasKey("Condition"))
        {
            Debug.LogWarning("Condition not found in PlayerPrefs. Using default value 1. " +
                             "Please set Condition using your login/parameter scene.");
            condition = 1;
            PlayerPrefs.SetInt("Condition", condition);
        }
        else
        {
            condition = PlayerPrefs.GetInt("Condition");
        }

        // Start at Phase 1
        currentPhase = 1;
        PlayerPrefs.SetInt("Phase", currentPhase);
        PlayerPrefs.Save();

        // Initialize flags
        isOrderNowClicked = false;
        isFoodSelected = false;
        isOrderingConfirmed = false;
        isSurveyCompleted = false;

        Debug.Log($"StateManagement initialized: Participant {participantID}, Condition {condition}, Phase {currentPhase}");

        // Apply condition settings
        ApplyConditionSettings();

        // Hide all UI objects at the beginning
        if (menu != null) HideObject(menu);
        if (food != null) HideObject(food);
        if (ordering != null) HideObject(ordering);
        if (survey != null) HideObject(survey);
        if (thankYou != null) HideObject(thankYou);

        // Initialize selectedOptions to have 5 entries
        selectedOptions = new int[5];
    }

    #region Condition & visual

    public void ApplyConditionSettings()
    {
        Debug.Log($"Applying settings for Condition {condition}");

        if (postProcessingVolume == null)
        {
            Debug.LogWarning("No Post Processing Volume assigned!");
            return;
        }

        UnityEngine.Rendering.Universal.ColorAdjustments colorAdjustments;
        if (!postProcessingVolume.profile.TryGet(out colorAdjustments))
        {
            Debug.LogError("ColorAdjustments not found in volume profile!");
            return;
        }

        UnityEngine.Rendering.Universal.WhiteBalance whiteBalance;
        if (!postProcessingVolume.profile.TryGet(out whiteBalance))
        {
            Debug.LogError("WhiteBalance not found in volume profile!");
            return;
        }

        switch (condition)
        {
            case 1:
                PlayAudio(0);
                whiteBalance.temperature.value = -20f;
                break;
            case 2:
                PlayAudio(0);
                whiteBalance.temperature.value = 20f;
                break;
            case 3:
                PlayAudio(1);
                whiteBalance.temperature.value = -20f;
                break;
            case 4:
                PlayAudio(1);
                whiteBalance.temperature.value = 20f;
                break;
        }

        // Sync participant/condition to ExperimentSession
        ExperimentSession session = ExperimentSession.Instance;
        if (session != null)
        {
            session.participantId = participantID.ToString();
            session.condition = condition.ToString();
        }
    }

    #endregion

    #region Phase control

    public void NextPhase()
    {
        if (currentPhase < 4)
        {
            currentPhase++;
            PlayerPrefs.SetInt("Phase", currentPhase);
            PlayerPrefs.Save();
            Debug.Log($"Advanced to Phase {currentPhase}");
        }
    }

    public void StartPhase2()
    {
        Debug.Log($"[State] StartPhase2 called. CurrentPhase = {currentPhase}");

        if (currentPhase == 1)
        {
            IsOrderNowClicked = true;
            NextPhase();
            Debug.Log("Phase 2 started: Food ordering.");
        }
        else
        {
            Debug.LogWarning($"StartPhase2 ignored. Expected Phase 1, but currentPhase = {currentPhase}");
        }
    }

    public void StartPhase3()
    {
        IsOrderingConfirmed = true;
        Debug.Log($"[StartPhase3] Phase 3 started on '{gameObject.name}' (InstanceID: {GetInstanceID()})");

        // Update ExperimentSession with selected food ID
        ExperimentSession session = ExperimentSession.Instance;
        if (session != null && !string.IsNullOrEmpty(selectedFoodId))
        {
            session.orderChoice = selectedFoodId;
        }

        SaveSessionData();

        if (ConfirmationAudioPlayer.Instance != null)
        {
            ConfirmationAudioPlayer.Instance.PlayConfirmation();
        }
        else
        {
            Debug.LogWarning("[StateManagement] ConfirmationAudioPlayer.Instance is NULL! No ConfirmationAudioPlayer in scene.");
        }

        IsSurveyCompleted = true;
        if (currentPhase < 4)
        {
            NextPhase();
        }

        if (agentDestinationSetter != null)
        {
            Debug.Log("Full sequence complete! Agent stays at destination.");
        }
    }

    public IEnumerator StartSurveyAfterAudio()
    {
        Debug.Log($"[State] StartSurveyAfterAudio: SKIPPING Survey. phase={currentPhase}, participant={participantID}, condition={condition}");

        // If agent ref wasn't wired, try to locate one to avoid hard failure.
        if (agent == null)
        {
            agent = FindAnyObjectByType<AgentDestinationSetter>();
            
        }

        // 1. Play Wrap Up Audio (Index 3 = Aufwiedersehen)
        if (agent != null)
        {
            // Note: We play index 3 (End) instead of 2 (Survey)
           // Debug.Log($"[State] Playing wrap-up via agent.PlayVoiceAndWait(3). agent={agent.name}");
            yield return StartCoroutine(agent.PlayVoiceAndWait(3));
        }
        else
        {
           // Debug.LogError("[StateManagement] 'Agent' reference is missing! Cannot play audio/anim.");
            yield return new WaitForSeconds(1f);
        }

        // 2. Do NOT show survey UI
        // if (survey != null) ShowObject(survey);
        // if (surveyManager != null) surveyManager.StartSurvey();

        // Mark flow as completed (skip-survey path)
        if (!IsSurveyCompleted)
        {
            IsSurveyCompleted = true;
        }

        // Advance to final phase if needed
        if (currentPhase < 4)
        {
            NextPhase();
        }

        if (currentPhase == 4)
        {
            Debug.Log("[State] Phase 4 reached (end screen). Showing Phase 4 UI if assigned.");
            ShowUIForPhase(4);
        }

        // 3. Save data immediately
       // Debug.Log("[State] Saving session data (skip-survey path)...");
        SaveSessionData();
        //Debug.Log("[State] Experiment Data Saved (Survey Skipped).");
    }

    public void MarkSurveyCompleted()
    {
        IsSurveyCompleted = true;
        NextPhase();
        //Debug.Log("Survey completed. Phase 4 (thank you) started.");

        // Write final data to CSV (centralized in StateManagement)
        SaveSessionData();
    }

    /// <summary>
    /// Writes a complete session data row from ExperimentSession.
    /// Initializes CSV file if needed, then writes all data in one call.
    /// </summary>
    public async void SaveSessionData()
    {
        ExperimentSession session = ExperimentSession.Instance;

        if (session == null)
        {
            Debug.LogError("[StateManagement] ExperimentSession.Instance is null!");
            return;
        }

        if (csvWriter == null)
        {
            Debug.LogError("[StateManagement] CSVWriter reference is null! Assign it in Inspector.");
            return;
        }

        // Initialize file if not already done
        if (!csvWriter.IsInitialized())
        {
            List<string> headers = new List<string>
            {
                "ParticipantID",
                "Condition",
                "OrderChoice",
                "Timestamp"
            };

            int participantIdInt = -1;
            int.TryParse(session.participantId, out participantIdInt);

            Debug.Log("[StateManagement] Initializing CSV file...");
            await csvWriter.InitializeFile(headers, participantIdInt);  // ✅ AWAIT here!
            Debug.Log($"[StateManagement] CSV initialized: {csvWriter.IsInitialized()}");
        }

        if (!csvWriter.IsInitialized())
        {
            Debug.LogError("[StateManagement] Could not initialize CSV file!");
            return;
        }

        // Prepare row data
        Dictionary<string, string> rowData = new Dictionary<string, string>
        {
            { "ParticipantID", session.participantId ?? "" },
            { "Condition", session.condition ?? "" },
            { "OrderChoice", session.orderChoice ?? "" },
            { "Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
        };

        Debug.Log($"[StateManagement] Writing row: PID={rowData["ParticipantID"]}, Condition={rowData["Condition"]}, Order={rowData["OrderChoice"]}");

        // Write the row
        csvWriter.WriteRow(rowData);
        Debug.Log($"[StateManagement] Session data saved for Participant {session.participantId}");
    }

    /// <summary>
    /// Explicit method for UI Buttons (like "Order Now") to call.
    /// Works well with VR Interaction SDK Unity Event Wrappers.
    /// </summary>
    public void OnOrderNowButtonPress()
    {
        Debug.Log("[StateManagement] OnOrderNowButtonPress triggered from UI.");
        StartPhase2();
    }

    #endregion

    #region Audio

    public void PlayAudio(int index)
    {
        if (audioClips == null || audioClips.Length == 0)
        {
            Debug.LogWarning("No audio clips assigned!");
            return;
        }

        if (index < 0 || index >= audioClips.Length)
        {
            Debug.LogWarning($"Invalid audio clip index: {index}. Must be between 0 and {audioClips.Length - 1}.");
            return;
        }

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.PlayOneShot(audioClips[index], clipsVolume);
        Debug.Log($"Playing audio clip: {audioClips[index].name} (Index: {index})");
    }

    #endregion

    #region Debug & Utils

    [ContextMenu("LOG ALL PREFS")]
    private void LogAllPlayerPrefs()
    {
        //Debug.Log("=== PlayerPrefs ===");
        //foreach (var key in PlayerPrefsUtility.GetAllKeys())
        //{
        //    Debug.Log($"[{key}] = {PlayerPrefs.GetString(key)}");
        //}
        //Debug.Log("===================");
    }

    public void HideObject(GameObject obj)
    {
        if (obj == null) return;
        if (!obj.activeSelf) return;

        obj.SetActive(false);
        Debug.Log($"{obj.name} hidden.");
    }

    public void ShowObject(GameObject obj)
    {
        if (obj == null) return;
        if (obj.activeSelf) return;

        obj.SetActive(true);
        Debug.Log($"{obj.name} shown.");
    }

    public void ShowUIForPhase(int phase)
    {
        switch (phase)
        {
            case 1:
                ShowObject(menu);
                break;
            case 2:
                if (menuManager != null)
                {
                    menuManager.PreparePhase2UI();
                }
                ShowObject(food);
                break;
            case 3:
                ShowObject(survey);
                break;
            case 4:
                ShowObject(thankYou);
                break;
        }
    }

    #endregion
}