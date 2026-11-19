
using UnityEngine;
using UniVRM10;

public class ClaraLipSync : MonoBehaviour
{
    [SerializeField] private Vrm10Instance claraVRM;
    [SerializeField] private AudioSource audioSource;

    [Header("Lip Sync Settings")]
    [SerializeField] private float sensitivity = 12f;
    [SerializeField] private float smoothSpeed = 15f;

    private readonly float[] samples = new float[256];
    private float mouthWeight;

    private void Awake()
    {
        if (claraVRM == null)
            claraVRM = GetComponent<Vrm10Instance>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        float targetWeight = 0f;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.GetOutputData(samples, 0);

            float sum = 0f;

            for (int i = 0; i < samples.Length; i++)
                sum += samples[i] * samples[i];

            // Calculate the audio's RMS amplitude.
            float rms = Mathf.Sqrt(sum / samples.Length);

            targetWeight = Mathf.Clamp01(rms * sensitivity);
        }

        // Smooth mouth movement.
        mouthWeight = Mathf.Lerp(
            mouthWeight,
            targetWeight,
            Time.deltaTime * smoothSpeed
        );

        // Apply mouth opening to Clara.
        claraVRM.Runtime.Expression.SetWeight(
            ExpressionKey.CreateFromPreset(ExpressionPreset.aa),
            mouthWeight
        );
    }
}
