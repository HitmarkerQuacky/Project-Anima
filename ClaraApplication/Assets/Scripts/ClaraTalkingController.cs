
using UnityEngine;

public class ClaraTalkingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource claraAudioSource;

    private static readonly int IsTalking =
        Animator.StringToHash("isTalking");

    private bool wasTalking;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (claraAudioSource == null)
            claraAudioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (animator == null || claraAudioSource == null)
            return;

        bool isTalking = claraAudioSource.isPlaying;

        if (isTalking != wasTalking)
        {
            animator.SetBool(IsTalking, isTalking);
            wasTalking = isTalking;
        }
    }
}
