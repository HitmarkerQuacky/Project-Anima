
using UnityEngine;
using UniVRM10;

public class ClaraExpressionController : MonoBehaviour
{
    [Header("Clara's VRM")]
    [SerializeField] private Vrm10Instance claraVRM;

    private readonly ExpressionPreset[] emotions =
    {
        ExpressionPreset.happy,
        ExpressionPreset.sad,
        ExpressionPreset.angry,
        ExpressionPreset.relaxed,
        ExpressionPreset.surprised
    };

    private void Awake()
    {
        if (claraVRM == null)
            claraVRM = GetComponent<Vrm10Instance>();
    }

    private void SetWeight(ExpressionPreset preset, float value)
    {
        claraVRM.Runtime.Expression.SetWeight(
            ExpressionKey.CreateFromPreset(preset),
            Mathf.Clamp01(value)
        );
    }

    public void SetEmotion(string emotion, float intensity = 0.7f)
    {
        // Clear any previously active emotions.
        foreach (ExpressionPreset preset in emotions)
            SetWeight(preset, 0f);

        switch (emotion.ToLowerInvariant())
        {
            case "happy":
                SetWeight(ExpressionPreset.happy, intensity);
                break;

            case "sad":
                SetWeight(ExpressionPreset.sad, intensity);
                break;

            case "angry":
                SetWeight(ExpressionPreset.angry, intensity);
                break;

            case "relaxed":
                SetWeight(ExpressionPreset.relaxed, intensity);
                break;

            case "surprised":
                SetWeight(ExpressionPreset.surprised, intensity);
                break;

            // Neutral is represented by clearing the emotions.
            case "neutral":
            default:
                break;
        }
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
    public void TestNeutral() => SetEmotion("neutral");
}
