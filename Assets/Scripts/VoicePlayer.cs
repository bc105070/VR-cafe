using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class VoicePlayer : MonoBehaviour
{
    public AudioClip[] voiceClips;
    private AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        // 确保不会因为 AudioSource 配置问题听不到
        src.playOnAwake = false;
        src.spatialBlend = 1.0f; // 1.0 = 3D sound (跟waiter位置), 0.0 = 2D (全场一样)
    }

    /// <summary>
    /// 播放指定索引的语音，并返回语音长度（秒）。
    /// 如果播放失败，返回 0。
    /// </summary>
    public float PlayVoice(int index)
    {
        if (voiceClips == null || index < 0 || index >= voiceClips.Length || voiceClips[index] == null)
        {
            Debug.LogWarning($"[VoicePlayer] Cannot play index {index}: Clip is missing or out of range.");
            return 0f;
        }

        src.PlayOneShot(voiceClips[index]);
        return voiceClips[index].length;
    }
}
