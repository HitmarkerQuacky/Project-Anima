
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class AvatarCameraController : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Transform avatarCamera;

    [Header("Orbit Settings")]
    [SerializeField] private float orbitSensitivity = 3f;
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 45f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSensitivity = 0.5f;
    [SerializeField] private float minDistance = 0.5f;
    [SerializeField] private float maxDistance = 6f;

    [Header("Preset Settings")]
    [SerializeField] private float transitionDuration = 0.5f;

    private float yaw = 180f;
    private float pitch = 0f;
    private float distance = 3.5f;

    private Coroutine presetTransition;

    private void Start()
    {
        UpdateCamera();
    }

    private void Update()
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        HandleOrbit();
        HandleZoom();
    }

    private void HandleOrbit()
    {
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * orbitSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * orbitSensitivity;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance -= scroll * zoomSensitivity * 10f;

            distance = Mathf.Clamp(
                distance,
                minDistance,
                maxDistance
            );

            UpdateCamera();
        }
    }

    private void UpdateCamera()
    {
        avatarCamera.localPosition =
            new Vector3(0f, 0f, -distance);
    }

    private void SetPreset(float height, float zoom)
    {
        if (presetTransition != null)
            StopCoroutine(presetTransition);

        presetTransition = StartCoroutine(
            TransitionToPreset(height, zoom)
        );
    }

    private IEnumerator TransitionToPreset(
        float targetHeight,
        float targetDistance)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float startDistance = distance;

        Vector3 targetPosition = new Vector3(
            startPosition.x,
            targetHeight,
            startPosition.z
        );

        Quaternion targetRotation =
            Quaternion.Euler(0f, 180f, 0f);

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / transitionDuration
            );

            // Smooth transition
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            distance = Mathf.Lerp(
                startDistance,
                targetDistance,
                t
            );

            UpdateCamera();

            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
        distance = targetDistance;

        yaw = 180f;
        pitch = 0f;

        UpdateCamera();
        presetTransition = null;
    }

    public void FacePreset()
    {
        SetPreset(1.55f, 0.75f);
    }

    public void UpperBodyPreset()
    {
        SetPreset(1.15f, 1.6f);
    }

    public void LowerBodyPreset()
    {
        SetPreset(0.45f, 1.6f);
    }

    public void FullBodyPreset()
    {
        SetPreset(0.9f, 3.5f);
    }
}
