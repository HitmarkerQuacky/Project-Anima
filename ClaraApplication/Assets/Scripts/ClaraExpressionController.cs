
using UnityEngine;
using UniVRM10;

public class ClaraExpressionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Vrm10Instance claraVRM;
    [SerializeField] private AudioSource claraAudioSource;

    [Header("Expression Settings")]
    [SerializeField] private float transitionTime = 0.3f;
    [SerializeField] private float holdAfterSpeech = 1.5f;

    private readonly ExpressionPreset[] emotions =
    {
        ExpressionPreset.happy,
        ExpressionPreset.sad,
        ExpressionPreset.angry,
        ExpressionPreset.relaxed,
        ExpressionPreset.surprised
    };

    private float[] currentWeights;
    private float[] targetWeights;
    private float[] velocities;

    private bool waitingForSpeechEnd;
    private float idleTimer = -1f;

    private void Awake()
    {
        if (claraVRM == null)
            claraVRM = GetComponent<Vrm10Instance>();

        if (claraAudioSource == null)
            claraAudioSource = GetComponent<AudioSource>();

        currentWeights = new float[emotions.Length];
        targetWeights = new float[emotions.Length];
        velocities = new float[emotions.Length];
    }

    private void Update()
    {
        UpdateExpressionWeights();
        UpdateSpeechState();
    }

    private void UpdateExpressionWeights()
    {
        for (int i = 0; i < emotions.Length; i++)
        {
            currentWeights[i] = Mathf.SmoothDamp(
                currentWeights[i],
                targetWeights[i],
                ref velocities[i],
                Mathf.Max(0.01f, transitionTime)
            );

            claraVRM.Runtime.Expression.SetWeight(
                ExpressionKey.CreateFromPreset(emotions[i]),
                Mathf.Clamp01(currentWeights[i])
            );
        }
    }

    private void UpdateSpeechState()
    {
        // Wait until the current audio finishes.
        if (waitingForSpeechEnd)
        {
            if (claraAudioSource != null &&
                !claraAudioSource.isPlaying)
            {
                waitingForSpeechEnd = false;
                idleTimer = holdAfterSpeech;
            }

            return;
        }

        // Count down only when a return to neutral
        // has been scheduled.
        if (idleTimer >= 0f)
        {
            idleTimer -= Time.deltaTime;

            if (idleTimer < 0f)
                ReturnToNeutral();
        }
    }

    public void SetEmotion(
        string emotion,
        float intensity = 0.7f)
    {
        // Cancel the previous response's idle timer.
        waitingForSpeechEnd = false;
        idleTimer = -1f;

        // Clear all target emotions.
        for (int i = 0; i < targetWeights.Length; i++)
            targetWeights[i] = 0f;

        if (string.IsNullOrEmpty(emotion))
            return;

        emotion = emotion.ToLowerInvariant().Trim();
        intensity = Mathf.Clamp01(intensity);

        for (int i = 0; i < emotions.Length; i++)
        {
            if (emotions[i].ToString() == emotion)
            {
                targetWeights[i] = intensity;
                break;
            }
        }
    }

    // Call immediately after audio playback starts.
    public void NotifySpeechStarted()
    {
        waitingForSpeechEnd = true;
        idleTimer = -1f;
    }

    // Use when the TTS request fails.
    public void NotifySpeechUnavailable()
    {
        waitingForSpeechEnd = false;
        idleTimer = holdAfterSpeech;
    }

    public void ReturnToNeutral()
    {
        waitingForSpeechEnd = false;
        idleTimer = -1f;

        for (int i = 0; i < targetWeights.Length; i++)
            targetWeights[i] = 0f;
    }

    [ContextMenu("Test Happy")]
    public void TestHappy() => SetEmotion("happy");

    [ContextMenu("Test Sad")]
    public void TestSad() => SetEmotion("sad");

    [ContextMenu("Test Angry")]
    public void TestAngry() => SetEmotion("angry");

    [ContextMenu("Test Relaxed")]
    public void TestRelaxed() => SetEmotion("relaxed");

    [ContextMenu("Test Surprised")]
    public void TestSurprised() => SetEmotion("surprised");

    [ContextMenu("Test Neutral")]
    public void TestNeutral() => ReturnToNeutral();
}
