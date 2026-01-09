using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

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
       // Reference to MenuManager
    public SurveyManager surveyManager;  // Reference to SurveyManager
    public MenuManager menuManager; 
    [Header("Status (Read Only in Inspector)")]
    public int participantID;
    public int condition;        // 1..4
    public int currentPhase;     // 1..4
    public bool isOrderNowClicked;
    public bool isFoodSelected;
    public bool isOrderingConfirmed;
    public bool isSurveyCompleted;

    // Public properties (now direct access via public fields)
    public bool IsOrderNowClicked { get => isOrderNowClicked; set => isOrderNowClicked = value; }
    public bool IsFoodSelected { get => isFoodSelected; set => isFoodSelected = value; }
    public bool IsOrderingConfirmed { get => isOrderingConfirmed; set => isOrderingConfirmed = value; }
    public bool IsSurveyCompleted { get => isSurveyCompleted; set => isSurveyCompleted = value; }

    // Inner data type for CSV
    // Moved to CsvWriter as public class

    public List<CsvWriter.ParticipantData> allParticipants = new List<CsvWriter.ParticipantData>();

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

        // Start at Phase 1
        currentPhase = 1;
        PlayerPrefs.SetInt("Phase", currentPhase);
        PlayerPrefs.Save();

        // Initialize flags
        isOrderNowClicked = false;
        isFoodSelected = false;
        isOrderingConfirmed = false;
        isSurveyCompleted = false;

        Debug.Log($"StateManagement initialized: Participant {participantID}, Phase {currentPhase}");

        // Load participant data and apply condition
        StartCoroutine(InitializeCondition());

        // Hide all UI objects at the beginning
        if (menu != null) HideObject(menu);
        if (food != null) HideObject(food);
        if (ordering != null) HideObject(ordering);
        if (survey != null) HideObject(survey);
    }

    #region Condition & visual

    public IEnumerator InitializeCondition()
    {
        allParticipants = CsvWriter.LoadParticipants(participantsCsvName);
        if (allParticipants.Count > 0)
        {
            ApplyConditionSettings();
        }
        yield return null;  // Ensure the coroutine yields
    }

    public void ApplyConditionSettings()
    {
        CsvWriter.ParticipantData participantData = allParticipants.Find(p => p.participantID == participantID);

        if (participantData == null)
        {
            Debug.LogWarning($"No condition found for Participant {participantID}. Using 1.");
            condition = 1;
        }
        else
        {
            condition = participantData.condition;
        }

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

        // 把 participant/condition 同步到 ExperimentSession，方便 CsvWriter 使用
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
            IsOrderNowClicked = true;   // AgentDestinationSetter 在等這個
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
            IsOrderingConfirmed = true; // AgentDestinationSetter 在等這個
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

        // Write final data to CSV (centralized here to avoid duplication)
        ExperimentSession session = ExperimentSession.Instance;
        if (session != null)
        {
            CsvWriter.WriteParticipantRow(session);
        }
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
        audioSource.volume = clipsVolume; // 仅应用到这里的音轨
        audioSource.Play();
        Debug.Log($"Playing audio clip {index}: {audioClips[index].name} at volume {clipsVolume}");
    }

    // 当你在 Inspector 里拖动滑块时，声音会立刻变化
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
