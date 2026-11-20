
using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UnityChatController : MonoBehaviour
{
    [Header("Python API")]
    [SerializeField] private string apiUrl = "http://127.0.0.1:8001";

    [Header("Chat UI")]
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private Transform messageContent;
    [SerializeField] private ScrollRect conversationScroll;

    [Header("Message Prefabs")]
    [SerializeField] private GameObject userMessagePrefab;
    [SerializeField] private GameObject claraMessagePrefab;

    [Header("Clara Audio")]
    [SerializeField] private AudioSource claraAudioSource;

    [Header("Clara Expressions")]
    [SerializeField] private ClaraExpressionController claraExpressionController;

    private bool isWaitingForResponse;

    [Serializable]
    private class ChatRequest
    {
        public string message;
    }

    [Serializable]
    private class ChatResponse
    {
        public string response;
        public string emotion;
        public float intensity;
    }

    [Serializable]
    private class HistoryMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class HistoryResponse
    {
        public HistoryMessage[] messages;
    }

    private void Start()
    {
        sendButton.onClick.AddListener(SendMessage);
        StartCoroutine(LoadHistory());
    }

    private void Update()
    {
        // Only handle Enter when the chat input is focused.
        if (!messageInput.isFocused)
            return;

        // Detect the Enter key (including numpad Enter).
        bool enterPressed =
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter);

        if (!enterPressed)
            return;

        // Check whether Shift is being held.
        bool shiftHeld =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        // Shift + Enter keeps the default newline behaviour.
        if (shiftHeld)
            return;

        // Enter alone sends the message.
        SendMessage();
    }

    public void SendMessage()
    {
        if (isWaitingForResponse)
        return;

        string text = messageInput.text.Trim();

        if (string.IsNullOrWhiteSpace(text))
            return;

        // Clear the input before sending.
        messageInput.SetTextWithoutNotify("");

        // Remove focus so Enter isn't processed again.
        messageInput.DeactivateInputField();

        // Display the user's message.
        CreateMessage(text, true);
        
        StartCoroutine(SendToPython(text));
    }

    private IEnumerator SendToPython(string text)
    {
        isWaitingForResponse = true;
        sendButton.interactable = false;

        ChatRequest data = new ChatRequest
        {
            message = text
        };

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(
            apiUrl + "/chat", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);

            request.uploadHandler =
                new UploadHandlerRaw(body);

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type", "application/json");

            request.timeout = 120;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ChatResponse response =
                    JsonUtility.FromJson<ChatResponse>(
                        request.downloadHandler.text
                    );

                // Display Clara's response in the chat.
                CreateMessage(response.response, false);

                // Apply the facial expression for Clara.
                if (claraExpressionController != null)
                {
                    claraExpressionController.SetEmotion(
                        response.emotion,
                        response.intensity
                    );
                }

                // Generate speech and play it through Unity.
                yield return StartCoroutine(
                    PlayClaraSpeech(response.response)
                    );
            }
            else
            {
                Debug.LogError(
                    "Clara API error: " + request.error
                );

                // Refresh from SQLite in case the server
                // processed the message before a network error.
                yield return StartCoroutine(LoadHistory());
            }
        }

        isWaitingForResponse = false;
        sendButton.interactable = true;
        messageInput.ActivateInputField();
    }

    private IEnumerator LoadHistory()
    {
        using (UnityWebRequest request =
            UnityWebRequest.Get(apiUrl + "/history"))
        {
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    "Could not load Clara history: " +
                    request.error
                );
                yield break;
            }

            HistoryResponse history =
                JsonUtility.FromJson<HistoryResponse>(
                    request.downloadHandler.text
                );

            // Clear previously displayed messages.
            foreach (Transform child in messageContent)
            {
                Destroy(child.gameObject);
            }

            if (history.messages != null)
            {
                foreach (HistoryMessage msg in history.messages)
                {
                    CreateMessage(
                        msg.content,
                        msg.role == "user"
                    );
                }
            }

            StartCoroutine(ScrollToBottom());
        }
    }

    private void CreateMessage(string text, bool isUser)
    {
        GameObject prefab = isUser
            ? userMessagePrefab
            : claraMessagePrefab;

        GameObject bubble = Instantiate(
            prefab, messageContent, false
        );

        TMP_Text messageText =
            bubble.GetComponentInChildren<TMP_Text>(true);

        if (messageText != null)
        {
            messageText.text = text;
        }

        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        // Wait for newly created UI elements to initialize.
        yield return null;

        // Force Unity to recalculate the conversation layout.
        Canvas.ForceUpdateCanvases();

        RectTransform contentRect =
            messageContent as RectTransform;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        Canvas.ForceUpdateCanvases();

        // Scroll to the bottom.
        conversationScroll.StopMovement();
        conversationScroll.verticalNormalizedPosition = 0f;

        // Allow the ScrollRect to update.
        yield return null;

        conversationScroll.verticalNormalizedPosition = 0f;
    }

    private IEnumerator PlayClaraSpeech(string text)
    {
        ChatRequest data = new ChatRequest
        {
            message = text
        };

        string json = JsonUtility.ToJson(data);

        string url = apiUrl + "/tts";

        using (UnityWebRequest request =
            new UnityWebRequest(url, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);

            request.uploadHandler =
                new UploadHandlerRaw(body);

            request.downloadHandler =
                new DownloadHandlerAudioClip(
                    url,
                    AudioType.WAV
                );

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            request.timeout = 300;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    "Clara TTS failed: " + request.error
                );

                if (claraExpressionController != null)
                {
                    claraExpressionController.NotifySpeechUnavailable();
                }

                yield break;
            }

            // Receive audio over HTTP and converts into AudioClip WAV file
            AudioClip clip =
                DownloadHandlerAudioClip.GetContent(request);

            claraAudioSource.Stop();

            AudioClip previousClip = claraAudioSource.clip;
            claraAudioSource.clip = clip;
            claraAudioSource.Play();

            if (claraExpressionController != null)
            {
                claraExpressionController.NotifySpeechStarted();
            }

            if (previousClip != null)
                Destroy(previousClip);
        }
    }


}
