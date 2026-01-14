using UnityEngine;

public class ExampleDebugTextCaller : MonoBehaviour
{
    public DebugCanvasManager debugCanvasManager;

    void Start()
    {
        if (debugCanvasManager != null)
        {
            debugCanvasManager.SetDebugText("Hello from another class!");
        }

    }
}
