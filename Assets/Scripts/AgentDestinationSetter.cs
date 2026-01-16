using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentDestinationSetter : MonoBehaviour
{
    

    public Transform destination;
    public Transform vrCamera;
    public float delayBeforeWalkingToDestination = 2f;
    public float delayBeforeReturning = 2f;
    public float arrivalRotationDistance = 2f;
    public string turnRightAnimationName = "TurnRight";
    public string turnLeftAnimationName = "TurnLeft";
    public string talkingAnimationName = "Talking";
    public float turnSpeed = 180f;
    public float talkingDuration = 3f;
    public bool startSequenceOnStart = true;

    public VoicePlayer voicePlayer;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform origin;
    private bool shouldFaceCamera = false;
    private bool isReturningToOrigin = false;
    private Coroutine currentSequence;
    public StateManagement stateManager;

    public DebugCanvasManager debugCanvasManager;

    void Start()
    {

       

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // NEW: Cache VoicePlayer reference (instead of StateManagement for audio)
    voicePlayer = GetComponent<VoicePlayer>();
    if (voicePlayer == null)
    {
        Debug.LogWarning("VoicePlayer not found on this GameObject! Audio will not play.");
    }

        if (vrCamera == null)
        {
            vrCamera = Camera.main?.transform;
            if (vrCamera == null)
            {
                Debug.LogWarning("VR Camera not found. Please assign it in the Inspector.");
            }
        }

        GameObject originMarker = new GameObject("Origin_" + gameObject.name);
        originMarker.transform.position = transform.position;
        originMarker.transform.rotation = transform.rotation;
        origin = originMarker.transform;

        // Only start sequence automatically if enabled
        if (startSequenceOnStart)
        {
            StartWalkSequence();
        }
    }

    void Update()
    {
        if (shouldFaceCamera && vrCamera != null && agent.enabled && !agent.isStopped)
        {
            if (agent.remainingDistance <= arrivalRotationDistance && agent.remainingDistance > agent.stoppingDistance)
            {
                RotateTowardsCamera();
            }
        }
    }

    // NEW: Public method to start the sequence (can be called from anywhere)
    public void StartWalkSequence()
    {
        // Stop any existing sequence first
        if (currentSequence != null)
        {
            StopCoroutine(currentSequence);
            ResetAgentState();
        }

        currentSequence = StartCoroutine(FullWalkSequence());
    }

    // NEW: Public method to stop the current sequence
    public void StopWalkSequence()
    {
        if (currentSequence != null)
        {
            StopCoroutine(currentSequence);
            currentSequence = null;
            ResetAgentState();
        }
    }

    // NEW: Reset agent to clean state
    private void ResetAgentState()
    {
        agent.isStopped = true;
        agent.ResetPath();
        shouldFaceCamera = false;
        isReturningToOrigin = false;
        
        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isTurningRight", false);
            animator.SetBool("isTurningLeft", false);
            animator.SetBool("isTalking", false);
        }
    }

       // 新增这个辅助协程：负责播语音、开动画、动态等待
    public IEnumerator PlayVoiceAndWait(int index)
    {

         if (debugCanvasManager != null)
        {
            // set a variiable containing the index as a string
            string debugMessage = $"voice index {index}.";
            debugCanvasManager.SetDebugText(debugMessage);
        }

        // 1. 播放并获取长度
        float clipLength = 0f;
        // Be robust: if not assigned in Inspector (or Start hasn't set it yet), resolve it now.
        voicePlayer = GetComponent<VoicePlayer>();
        if (voicePlayer == null)
        {
            Debug.LogWarning("VoicePlayer not found on this GameObject! Audio will not play.");

            // NEW: Debug canvas output (same style)
            if (debugCanvasManager != null)
            {
                string debugMessage = "[Agent] VoicePlayer was null in Start(), attempting to get it from component (GetComponent<VoicePlayer> returned null).";
                debugCanvasManager.SetDebugText(debugMessage);
            }
        }
        

         
        if (voicePlayer != null)
        {
          
            clipLength = voicePlayer.PlayVoice(index);
        }
        else
        {
            Debug.LogWarning("[Agent] VoicePlayer is missing. Cannot play voice clip.");
        }

        // 2. 开启说话动画
        if (animator != null) animator.SetBool("isTalking", true);

        // 3. 动态等待时长（如果语音很长就等语音，如果语音很短至少做2秒动作）
    float waitTime = Mathf.Max(clipLength, 2f);

    if (debugCanvasManager != null)
    {
        string debugMessage = $"[Agent] Waiting {waitTime:0.00}s (clipLength={clipLength:0.00}s, index={index}).";
        debugCanvasManager.SetDebugText(debugMessage);
    }
        yield return new WaitForSeconds(waitTime);

       // 4. 关闭说话动画
    if (animator != null)
    {
        animator.SetBool("isTalking", false);

        if (debugCanvasManager != null)
        {
            string debugMessage = $"[Agent] Talking animation OFF (voice index {index}).";
            debugCanvasManager.SetDebugText(debugMessage);
        }
    }

        yield return new WaitForSeconds(0.2f); // 稍微缓冲一下
    }


    // 替换掉你原来的 FullWalkSequence
    private IEnumerator FullWalkSequence()
    {
          // ================= Phase 1: Greeting =================


yield return new WaitForSeconds(delayBeforeWalkingToDestination);
yield return StartCoroutine(WalkToDestinationCoroutine(destination, true));
yield return StartCoroutine(WaitForArrival());

Debug.Log($"[Agent] stateManager = {(stateManager == null ? "NULL" : "OK")}");
if (stateManager != null)
{
    Debug.Log($"[Agent] stateManager.menu = {(stateManager.menu == null ? "NULL" : stateManager.menu.name)}");
}

if (stateManager == null)
{
    Debug.LogError("[Agent] stateManager 为 null，无法显示菜单！");
}
else if (stateManager.menu == null)
{
    Debug.LogError("[Agent] stateManager.menu 为 null，没拖 Menu_Read 进去！");
}
else
{
    Debug.Log("[Agent] 调用 ShowUIForPhase(1)...");
    stateManager.ShowUIForPhase(1);  // 統一顯示Phase 1 UI
}


// 播欢迎语音(0)并动态等待
yield return StartCoroutine(PlayVoiceAndWait(0));






yield return new WaitForSeconds(delayBeforeReturning);
isReturningToOrigin = true;
yield return StartCoroutine(TurnAndWalkTo(origin, false, turnLeftAnimationName));
yield return StartCoroutine(WaitForArrival());
isReturningToOrigin = false;


// 等待 "Order Now" 按钮
Debug.Log("Waiting for Order Now button click...");
yield return new WaitUntil(() => stateManager.IsOrderNowClicked);
Debug.Log("Order Now clicked, starting Phase 2"); 


        // ================= Phase 2: Food Ordering =================
        yield return new WaitForSeconds(delayBeforeWalkingToDestination);
        yield return StartCoroutine(WalkToDestinationCoroutine(destination, true));
        yield return StartCoroutine(WaitForArrival());

        // 先弹食物选项 - 統一顯示Phase 2 UI
        if (stateManager != null) stateManager.ShowUIForPhase(2);

        // 播点菜提示(1)并动态等待
        yield return StartCoroutine(PlayVoiceAndWait(1));

           // ✅ 优化：等待 Yes 按钮点击（IsOrderingConfirmed）
          
   

     
    //Debug.Log("[Phase 2] Waiting for Yes button (ordering confirmation)...");
    yield return new WaitUntil(() => stateManager != null && stateManager.IsOrderingConfirmed);
    Debug.Log("[Phase 2] Yes button clicked! Order confirmed.");
    if (debugCanvasManager != null)
    {
        // set a variiable containing the index as a string
        string debugMessage = "Phase 2]  Yes button (ordering confirmation)...";

        debugCanvasManager.SetDebugText(debugMessage);
    }
// ✅ 新增：隐藏食物选择界面 - 統一隱藏
    //if (stateManager != null && stateManager.food != null)
      //  {
        //stateManager.HideObject(stateManager.food);
        //Debug.Log("[Phase 2] Food menu hidden");
        //}

        // ================= Phase 3 & 4: Wrap up =================
        // Skipping Survey: play a final voice line, then save CSV
        if (stateManager != null)
        {
            // ✅ Use ConfirmationAudioPlayer instead of StateManagement methods
            if (stateManager.confirmationAudioPlayer != null)
            {
                // 1) Play confirmation audio
                stateManager.confirmationAudioPlayer.PlayConfirmation();
                
                // 2) Wait for the audio to finish
                float audioLength = stateManager.confirmationAudioPlayer.GetClipLength();
                if (audioLength > 0)
                {
                    yield return new WaitForSeconds(audioLength);
                }
                
                Debug.Log("[Agent] Confirmation audio finished, proceeding to wrap-up.");
                
                if (debugCanvasManager != null)
                {
                    debugCanvasManager.SetDebugText("[Agent] Confirmation audio.finished, now saving CSV...");
                }
            }
            else
            {
                Debug.LogWarning("[Agent] ConfirmationAudioPlayer not assigned in StateManagement!");
            }

            // 2) Mark completion + advance to Phase 4
            stateManager.IsSurveyCompleted = true;
            if (stateManager.currentPhase < 4)
            {
                stateManager.NextPhase();
            }

            // 3) Show end screen (if assigned) and save CSV
            stateManager.SaveSessionData();
        }
        else
        {
            Debug.LogError("[Agent] stateManager is NULL. Cannot run wrap-up/save.");
        }

        currentSequence = null;
        Debug.Log("Full sequence complete! Agent stays at destination.");
    }

    private string ShowMenuWithDelay(float v)
    {
        throw new NotImplementedException();
    }

    private IEnumerator WaitForArrival()
    {
        while (!agent.hasPath || agent.pathPending)
        {
            yield return null;
        }

        while (agent.remainingDistance > Mathf.Max(agent.stoppingDistance, 0.5f))
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator PlayTalkingAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("isTalking", true);

            // if (stateManager != null)
            // {
            //     stateManager.PlayAudioForCurrentPhase();
            // }
            
            if (voicePlayer != null)
            {
                float clipLength = voicePlayer.PlayVoice(0); // Or appropriate index
                talkingDuration = Mathf.Max(clipLength, talkingDuration);
            }
            
            float duration = talkingDuration;
            yield return new WaitForSeconds(duration);
            animator.SetBool("isTalking", false);
            yield return new WaitForSeconds(0.2f);

        }
    }

    private void RotateTowardsCamera()
    {
        Vector3 directionToCamera = vrCamera.position - transform.position;
        directionToCamera.y = 0;

        if (directionToCamera.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            NavAgentAnimatorSync animSync = GetComponent<NavAgentAnimatorSync>();
            float rotSpeed = animSync != null ? animSync.turnSpeed : 120f;
            
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotSpeed * Time.deltaTime
            );
        }
    }

    private IEnumerator WalkToDestinationCoroutine(Transform target, bool faceCamera)
    {
        shouldFaceCamera = faceCamera;
        yield return StartCoroutine(TurnAndWalkTo(target, faceCamera, turnRightAnimationName));
    }

    public void WalkTo(Transform target, bool faceCamera = false)
    {
        if (target != null && agent != null)
        {
            shouldFaceCamera = faceCamera;
            StartCoroutine(TurnAndWalkTo(target, faceCamera, turnRightAnimationName));
        }
    }

    public void WalkToOrigin()
    {
        StartCoroutine(TurnAndWalkTo(origin, false, turnLeftAnimationName));
    }

    private IEnumerator TurnAndWalkTo(Transform target, bool faceCamera, string turnAnimationName)
    {
        agent.isStopped = true;
        agent.ResetPath();

        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0;

        if (directionToTarget.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            
            // NEW: Use the requested animation, not the calculated shortest path
            bool requestedTurnLeft = turnAnimationName == turnLeftAnimationName;
            string animBoolName = requestedTurnLeft ? "isTurningLeft" : "isTurningRight";

            if (animator != null)
            {
                animator.SetBool(animBoolName, true);
            }

            float turnDuration = GetAnimationClipLength(turnAnimationName);
            float elapsed = 0f;

            while (elapsed < turnDuration || Quaternion.Angle(transform.rotation, targetRotation) > 1f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.rotation = targetRotation;

            if (animator != null)
            {
                animator.SetBool(animBoolName, false);
            }
            
            yield return new WaitForSeconds(0.1f);
        }

        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    private float GetAnimationClipLength(string clipName)
    {
        if (animator == null) return 0f;

        RuntimeAnimatorController ac = animator.runtimeAnimatorController;
        foreach (AnimationClip clip in ac.animationClips)
        {
            if (clip.name == clipName)
            {
                return clip.length;
            }
        }

        Debug.LogWarning($"Animation clip '{clipName}' not found. Using default duration.");
        return 0.5f;
    }

    private IEnumerator ShowMenuAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay); // 独立等待，不影响主线
    if (stateManager != null)
    {
        stateManager.ShowUIForPhase(1);  // 統一顯示
    }
}
/// <summary>
/// 只播放语音，不等待，不控制动画（适合快速触发）
/// </summary>
private void PlayVoiceOnly(int index)
{
    if (voicePlayer == null)
    {
        voicePlayer = GetComponent<VoicePlayer>();
    }

    if (voicePlayer != null)
    {
        voicePlayer.PlayVoice(index);
        Debug.Log($"[Agent] Playing voice index {index} (no wait).");
    }
    else
    {
        Debug.LogWarning("[Agent] VoicePlayer not found, cannot play voice.");
    }
}
}