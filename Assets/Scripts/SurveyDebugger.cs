using UnityEngine;

public class SurveyDebugger : MonoBehaviour
{
    private void OnEnable()
    {
        Debug.Log("[SurveyDebugger] Survey ENABLED at time " + Time.time);
    }

    private void OnDisable()
    {
        Debug.Log("[SurveyDebugger] Survey DISABLED at time " + Time.time);
    }
}
