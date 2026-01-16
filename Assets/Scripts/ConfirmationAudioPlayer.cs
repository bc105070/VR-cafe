using UnityEngine;

/// <summary>
/// Simple dedicated audio player for confirmation sounds.
/// Attach to a GameObject in the scene and reference it from anywhere.
/// </summary>
public class ConfirmationAudioPlayer : MonoBehaviour
{
    public static ConfirmationAudioPlayer Instance { get; private set; }

    [Header("Audio")]
    [Tooltip("The audio clip to play when order is confirmed")]
    public AudioClip confirmationClip;
    
    [Range(0f, 1f)]
    public float volume = 0.7f;

    private AudioSource audioSource;

    private void Awake()
    {
        Debug.Log($"[ConfirmationAudioPlayer] Awake on '{gameObject.name}'");
        
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"[ConfirmationAudioPlayer] ✓ Instance set: {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[ConfirmationAudioPlayer] ⚠️ Duplicate instance found on '{gameObject.name}'. Destroying this one. Existing instance: {Instance.gameObject.name}");
            Destroy(gameObject);
            return;
        }

        // Get or create AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
        
        Debug.Log("[ConfirmationAudioPlayer] ✓ Initialized successfully");
    }

    /// <summary>
    /// Plays the confirmation audio clip.
    /// </summary>
    public void PlayConfirmation()
    {
        if (confirmationClip == null)
        {
            Debug.LogWarning("[ConfirmationAudioPlayer] No audio clip assigned!");
            return;
        }

        audioSource.PlayOneShot(confirmationClip, volume);
        Debug.Log($"[ConfirmationAudioPlayer] Playing: {confirmationClip.name}");
    }

    /// <summary>
    /// Returns the length of the audio clip in seconds.
    /// </summary>
    public float GetClipLength()
    {
        return confirmationClip != null ? confirmationClip.length : 0f;
    }
}