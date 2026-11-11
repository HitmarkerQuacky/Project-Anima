
using UnityEngine;

public class ChatWindowController : MonoBehaviour
{
    [SerializeField] private GameObject chatWindow;

    public void ToggleChat()
    {
        chatWindow.SetActive(!chatWindow.activeSelf);
    }
}
