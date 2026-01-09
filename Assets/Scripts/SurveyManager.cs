using UnityEngine;
using UnityEngine.Events;

public class SurveyManager : MonoBehaviour
{
    public GameObject[] surveyPanels;    // e.g. Survey1, Survey2, Survey3, Survey4, Survey5
    public UnityEvent onSurveyFinished;  // Event triggered when survey is completed

    public StateManagement stateManager;  // Reference to StateManagement for audio
    public int endAudioIndex = 3;  // Index of end audio clip

    public int currentSurveyIndex = -1;

    private void Awake()
    {
        Debug.Log("[SurveyManager] Awake");

        if (surveyPanels == null || surveyPanels.Length == 0)
        {
            Debug.LogWarning("[SurveyManager] surveyPanels is null or empty in Awake");
            return;
        }

        // Initialize selectedOptions in StateManagement if needed
        if (stateManager != null)
        {
            if (stateManager.selectedOptions == null || stateManager.selectedOptions.Length != surveyPanels.Length)
            {
                stateManager.selectedOptions = new int[surveyPanels.Length];
                Debug.Log($"[SurveyManager] Initialized selectedOptions array in StateManagement with length {surveyPanels.Length}");
            }
        }
        else
        {
            Debug.LogError("[SurveyManager] StateManagement reference is null! Cannot initialize selectedOptions.");
        }

        foreach (var p in surveyPanels)
        {
            if (p != null)
            {
                Debug.Log("[SurveyManager] Hide panel in Awake: " + p.name);
                p.SetActive(false);
            }
        }
    }

    private void Start()
    {
        Debug.Log("[SurveyManager] Start - waiting for trigger to start survey");
        // Removed automatic StartSurvey()
    }

    public void StartSurvey()
    {
        Debug.Log("[SurveyManager] StartSurvey()");
        currentSurveyIndex = 0;
        ShowCurrentSurvey();
    }

    public void ChooseOption(int optionIndex)
    {
        Debug.Log("[SurveyManager] ChooseOption(" + optionIndex + ")");

        if (stateManager == null)
        {
            Debug.LogError("[SurveyManager] StateManagement reference is null!");
            return;
        }

        if (currentSurveyIndex < 0 || currentSurveyIndex >= surveyPanels.Length)
        {
            Debug.LogWarning("[SurveyManager] invalid currentSurveyIndex: " + currentSurveyIndex);
            return;
        }

        if (stateManager.selectedOptions == null || currentSurveyIndex >= stateManager.selectedOptions.Length)
        {
            Debug.LogError("[SurveyManager] selectedOptions array in StateManagement is null or too small!");
            return;
        }

        // Store selection in StateManagement
        stateManager.selectedOptions[currentSurveyIndex] = optionIndex;

        // Hide current
        if (surveyPanels[currentSurveyIndex] != null)
        {
            Debug.Log("[SurveyManager] Hide panel: " + surveyPanels[currentSurveyIndex].name);
            surveyPanels[currentSurveyIndex].SetActive(false);
        }

        // Next
        currentSurveyIndex++;

        if (currentSurveyIndex < surveyPanels.Length)
        {
            ShowCurrentSurvey();
        }
        else
        {
            Debug.Log("[SurveyManager] All surveys finished");
            // Save survey answers to ExperimentSession
            ExperimentSession session = ExperimentSession.Instance;
            if (session != null && stateManager.selectedOptions.Length >= 5)
            {
                session.q1Choice = stateManager.selectedOptions[0].ToString();
                session.q2Choice = stateManager.selectedOptions[1].ToString();
                session.q3Choice = stateManager.selectedOptions[2].ToString();
                session.q4Choice = stateManager.selectedOptions[3].ToString();
                session.q5Choice = stateManager.selectedOptions[4].ToString();
                Debug.Log("[SurveyManager] Survey answers saved to ExperimentSession");
            }

            // Play end audio
            if (stateManager != null)
            {
                stateManager.PlayAudio(endAudioIndex);
                stateManager.MarkSurveyCompleted();  // Centralized completion handling
            }

            onSurveyFinished?.Invoke();
        }
    }

    public void ShowCurrentSurvey()
    {
        if (currentSurveyIndex >= 0 && currentSurveyIndex < surveyPanels.Length)
        {
            var panel = surveyPanels[currentSurveyIndex];
            if (panel != null)
            {
                Debug.Log("[SurveyManager] Show panel: " + panel.name);
                panel.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[SurveyManager] panel is null at index " + currentSurveyIndex);
            }
        }
        else
        {
            Debug.LogWarning("[SurveyManager] ShowCurrentSurvey with invalid index: " + currentSurveyIndex);
        }
    }
}