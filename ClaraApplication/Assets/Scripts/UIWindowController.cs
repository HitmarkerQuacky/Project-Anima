
using UnityEngine;

public class UIWindowController : MonoBehaviour
{
    [Header("UI Windows")]
    [SerializeField] private GameObject chatWindow;
    [SerializeField] private GameObject cameraPresetsWindow;

    [Header("Camera")]
    [SerializeField] private AvatarCameraController cameraController;

    private void Awake()
    {
        // Hide camera presets when the application starts.
        cameraPresetsWindow.SetActive(false);

        // Disable camera controls initially.
        cameraController.enabled = false;
    }

    public void ToggleChat()
    {
        chatWindow.SetActive(!chatWindow.activeSelf);
    }

    public void ToggleCamera()
    {
        // Toggle the camera presets window.
        bool isOpen = !cameraPresetsWindow.activeSelf;

        cameraPresetsWindow.SetActive(isOpen);

        // Enable camera controls only when presets are visible.
        cameraController.enabled = isOpen;
    }
}
