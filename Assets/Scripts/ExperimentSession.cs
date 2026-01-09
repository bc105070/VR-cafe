using UnityEngine;

/// <summary>
/// Holds participant data, order choice, and survey answers for the current session.
/// Implemented as a simple singleton that survives scene changes.
/// </summary>
public class ExperimentSession : MonoBehaviour
{
    public static ExperimentSession Instance { get; private set; }

    [Header("Participant")]
    public string participantId;   // e.g. "001", "P12"
    public string condition;       // e.g. "1", "2", "cold", "warm"

    [Header("Order")]
    public string orderChoice;     // e.g. "Set1", "Dessert", etc.

    [Header("Survey Answers")]
    public string q1Choice;
    public string q2Choice;
    public string q3Choice;
    public string q4Choice;
    public string q5Choice;

    private void Awake()
    {
        // Singleton + persist across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
