using TMPro;
using UnityEngine;

public class DebugCanvasManager : MonoBehaviour
{
    [Header("Debug UI")]
    public TMP_Text debugText;

    public void SetDebugText(string message)
    {
        if (debugText == null)
        {
            Debug.LogWarning("DebugCanvasManager: debugText is not assigned.");
            return;
        }

        debugText.text = message;
    }
}
