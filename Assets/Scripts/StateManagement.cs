using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages global state: participant ID/condition, phases,
/// and flags for AgentDestinationSetter & MenuManager.
/// </summary>
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
    public GameObject menu;      // Menu_Read root
    public GameObject food;      // Menu_Order root
    public GameObject ordering;  // (optional) extra ordering UI
    public GameObject survey;    // Survey root

    [Header("Managers")]
    public MenuManager menuManager;
    public SurveyManager surveyManager;
    public CSVWriter csvWriter;

    [Header("Status (Read Only in Inspector)")]
    public int participantID;
    public int condition;        // 1..4
    public int currentPhase;     // 1..4

    public string selectedFoodId = null;
    public int[] selectedOptions;

    public bool isOrderNowClicked;
    public bool isFoodSelected;
    public bool isOrderingConfirmed;
    public bool isSurveyCompleted;

    // Public properties (now direct access via public fields)
    public bool IsOrderNowClicked { get => isOrderNowClicked; set => isOrderNowClicked = value; }
    public bool IsFoodSelected { get => isFoodSelected; set => isFoodSelected = value; }
    public bool IsOrderingConfirmed { get => isOrderingConfirmed; set => isOrderingConfirmed = value; }
    public bool IsSurveyCompleted { get => isSurveyCompleted; set => isSurveyCompleted = value; }

    private void Start()
    {
        Debug.Log("StateManagement is alive!");

        // Participant ID is stored in PlayerPrefs (set by your login / parameter scene)
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

        // Get condition from PlayerPrefs (should be set by your parameter scene)
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

        // Initialize selectedOptions to have 5 entries (for 5 survey questions)
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
        Debug.Log($"[State] StartPhase3 called. CurrentPhase = {currentPhase}");

        if (currentPhase == 2)
        {
            IsOrderingConfirmed = true;
            NextPhase();
            Debug.Log("Phase 3 started: Survey.");

            // Play ordering completion audio and start survey
            StartCoroutine(StartSurveyAfterAudio());
        }
        else
        {
            Debug.LogWarning($"StartPhase3 ignored. Expected Phase 2, but currentPhase = {currentPhase}");
        }
    }

    public IEnumerator StartSurveyAfterAudio()
    {
        // Play audio for Phase 3 (ordering completion)
        PlayAudioForCurrentPhase();

        // Wait for audio to finish
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            yield return new WaitForSeconds(audioSource.clip.length);
        }
        else
        {
            yield return new WaitForSeconds(1f); // Fallback wait
        }

        // Show survey UI
        if (survey != null)
        {
            ShowObject(survey);
        }

        // Start survey
        if (surveyManager != null)
        {
            surveyManager.StartSurvey();
        }
        else
        {
            Debug.LogWarning("SurveyManager not assigned in StateManagement!");
        }
    }

    public void MarkSurveyCompleted()
    {
        IsSurveyCompleted = true;
        NextPhase();
        Debug.Log("Survey completed. Phase 4 (thank you) started.");

        // Write final data to CSV (centralized in StateManagement)
        SaveSessionData();
    }

    /// <summary>
    /// Writes a complete session data row from ExperimentSession.
    /// Initializes CSV file if needed, then writes all data in one call.
    /// </summary>
    public void SaveSessionData()
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
                "Q1_Choice",
                "Q2_Choice",
                "Q3_Choice",
                "Q4_Choice",
                "Q5_Choice",
                "Timestamp"
            };

            int participantIdInt = -1;
            int.TryParse(session.participantId, out participantIdInt);

            csvWriter.InitializeFile(headers, participantIdInt);
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
            { "Q1_Choice", session.q1Choice ?? "" },
            { "Q2_Choice", session.q2Choice ?? "" },
            { "Q3_Choice", session.q3Choice ?? "" },
            { "Q4_Choice", session.q4Choice ?? "" },
            { "Q5_Choice", session.q5Choice ?? "" },
            { "Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
        };

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
            Debug.LogWarning($"Audio clip index {index} out of range!");
            return;
        }

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.clip = audioClips[index];
        audioSource.volume = clipsVolume;
        audioSource.Play();
        Debug.Log($"Playing audio clip {index}: {audioClips[index].name} at volume {clipsVolume}");
    }

    private void OnValidate()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.volume = clipsVolume;
        }
    }

    public void PlayAudioForCurrentPhase()
    {
        int audioIndex = currentPhase - 1; // Phase1->0, Phase2->1,...

        if (audioClips == null || audioClips.Length == 0)
        {
            Debug.LogWarning("No audio clips assigned!");
            return;
        }

        if (audioIndex < 0 || audioIndex >= audioClips.Length || audioClips[audioIndex] == null)
        {
            Debug.LogWarning($"No clip for Phase {currentPhase} (index {audioIndex})");
            return;
        }

        PlayAudio(audioIndex);
    }

    #endregion

    #region Show / Hide helpers

    public void ShowObject(GameObject obj)
    {
        if (obj == null) return;
        if (obj.activeSelf) return;

        obj.SetActive(true);
        Debug.Log($"{obj.name} shown.");
    }

    public void HideObject(GameObject obj)
    {
        if (obj == null) return;
        if (!obj.activeSelf) return;

        obj.SetActive(false);
        Debug.Log($"{obj.name} hidden.");
    }

    public void ShowObjectForCurrentPhase()
    {
        switch (currentPhase)
        {
            case 1:
                ShowObject(menu);
                break;
            case 2:
                if (menuManager != null)
                {
                    menuManager.PreparePhase2UI();
                }
                else
                {
                    Debug.LogWarning("MenuManager not assigned in StateManagement! Cannot prepare Phase 2 UI.");
                }
                ShowObject(food);
                break;
            case 3:
                ShowObject(survey);
                break;
            case 4:
                // thank-you only
                break;
        }
    }

    /// <summary>
    /// Show UI for a specific phase (used by AgentDestinationSetter)
    /// </summary>
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
                // thank-you only
                break;
        }
    }

    #endregion
}