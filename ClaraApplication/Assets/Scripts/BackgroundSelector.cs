
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundSelector : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameObject selectorBar;
    [SerializeField] private Image backgroundLayer;
    [SerializeField] private TMP_Text selectedFileName;

    [Header("Carousel References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private HorizontalLayoutGroup layoutGroup;
    [SerializeField] private BackgroundThumbnail thumbnailPrefab;

    [Header("Carousel Appearance")]
    [SerializeField] private float thumbnailWidth = 250f;
    [SerializeField] private float centreDuration = 0.3f;

    private const string SaveKey = "ClaraSelectedBackground";

    private string folderPath;
    private string selectedName = "";

    private readonly List<string> fileNames = new List<string>();
    private readonly List<BackgroundThumbnail> thumbnails =
        new List<BackgroundThumbnail>();

    private readonly Dictionary<string, Sprite> sprites =
        new Dictionary<string, Sprite>();

    private Coroutine centreCoroutine;

    private void Awake()
    {
        folderPath = Path.Combine(
            Application.persistentDataPath,
            "Backgrounds"
        );

        Directory.CreateDirectory(folderPath);

        selectedName = PlayerPrefs.GetString(SaveKey, "");

        Debug.Log("Background folder: " + folderPath);
    }

    private void Start()
    {
        // Restore the previously selected wallpaper.
        if (!string.IsNullOrEmpty(selectedName))
            ApplyBackground(selectedName, false);

        UpdateFileName();

        selectorBar.SetActive(false);
    }

    public void ToggleSelector()
    {
        if (selectorBar.activeSelf)
        {
            CloseSelector();
        }
        else
        {
            OpenSelector();
        }
    }

    public void OpenSelector()
    {
        selectorBar.SetActive(true);
        RefreshGallery();
    }

    public void CloseSelector()
    {
        if (centreCoroutine != null)
        {
            StopCoroutine(centreCoroutine);
            centreCoroutine = null;
        }

        selectorBar.SetActive(false);
    }

    public void RefreshGallery()
    {
        if (centreCoroutine != null)
        {
            StopCoroutine(centreCoroutine);
            centreCoroutine = null;
        }

        scrollRect.StopMovement();

        // Remove old thumbnail objects.
        foreach (BackgroundThumbnail thumbnail in thumbnails)
        {
            if (thumbnail != null)
                Destroy(thumbnail.gameObject);
        }

        thumbnails.Clear();
        fileNames.Clear();

        string[] files = Directory.GetFiles(folderPath);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        foreach (string path in files)
        {
            string extension =
                Path.GetExtension(path).ToLowerInvariant();

            if (extension != ".png" &&
                extension != ".jpg" &&
                extension != ".jpeg")
                continue;

            Sprite sprite = LoadSprite(path);

            if (sprite == null)
                continue;

            string fileName = Path.GetFileName(path);

            fileNames.Add(fileName);

            BackgroundThumbnail thumbnail = Instantiate(
                thumbnailPrefab,
                content,
                false
            );

            string capturedName = fileName;

            thumbnail.Setup(
                sprite,
                () => SelectBackground(capturedName),
                scrollRect
            );

            thumbnails.Add(thumbnail);
        }

        // If there isn't a valid saved selection,
        // select the first available background.
        if (!fileNames.Contains(selectedName))
        {
            selectedName = "";

            if (fileNames.Count > 0)
                ApplyBackground(fileNames[0], true);
            else
                PlayerPrefs.DeleteKey(SaveKey);
        }

        UpdateSelectionBorders();
        UpdateFileName();

        StartCoroutine(CentreAfterLayout());
    }

    private Sprite LoadSprite(string path)
    {
        if (sprites.TryGetValue(path, out Sprite cached))
            return cached;

        Texture2D texture = null;

        try
        {
            byte[] imageData = File.ReadAllBytes(path);

            texture = new Texture2D(
                2, 2, TextureFormat.RGBA32, false
            );

            if (!ImageConversion.LoadImage(
                texture, imageData, true))
            {
                Destroy(texture);
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(
                    0, 0,
                    texture.width,
                    texture.height
                ),
                new Vector2(0.5f, 0.5f)
            );

            sprites.Add(path, sprite);

            return sprite;
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                "Failed to load " + path + ": " + error.Message
            );

            if (texture != null)
                Destroy(texture);

            return null;
        }
    }

    public void SelectBackground(string fileName)
    {
        if (!ApplyBackground(fileName, true))
            return;

        UpdateSelectionBorders();
        UpdateFileName();
        CentreSelected(true);
    }

    private bool ApplyBackground(string fileName, bool save)
    {
        string path = Path.Combine(folderPath, fileName);

        if (!File.Exists(path))
            return false;

        Sprite sprite = LoadSprite(path);

        if (sprite == null)
            return false;

        backgroundLayer.sprite = sprite;
        backgroundLayer.color = Color.white;

        selectedName = fileName;

        if (save)
        {
            PlayerPrefs.SetString(SaveKey, selectedName);
            PlayerPrefs.Save();
        }

        return true;
    }

    private void UpdateSelectionBorders()
    {
        for (int i = 0; i < thumbnails.Count; i++)
        {
            thumbnails[i].SetSelected(
                fileNames[i] == selectedName
            );
        }
    }

    private void UpdateFileName()
    {
        if (selectedFileName == null)
            return;

        selectedFileName.text =
            string.IsNullOrEmpty(selectedName)
            ? "No background selected"
            : selectedName;
    }

    private IEnumerator CentreAfterLayout()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        // Pad both ends so even the first and last
        // thumbnail can reach the centre.
        float viewportWidth = scrollRect.viewport.rect.width;

        int padding = Mathf.Max(
            0,
            Mathf.RoundToInt(
                (viewportWidth - thumbnailWidth) / 2f
            )
        );

        layoutGroup.padding.left = padding;
        layoutGroup.padding.right = padding;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        CentreSelected(false);
    }

    private void CentreSelected(bool animated)
    {
        int index = fileNames.IndexOf(selectedName);

        if (index < 0)
            return;

        float spacing = layoutGroup.spacing;

        float targetOffset =
            index * (thumbnailWidth + spacing);

        float availableWidth = Mathf.Max(
            0f,
            content.rect.width - scrollRect.viewport.rect.width
        );

        float targetNormalized = availableWidth > 0f
            ? Mathf.Clamp01(targetOffset / availableWidth)
            : 0f;

        if (centreCoroutine != null)
            StopCoroutine(centreCoroutine);

        scrollRect.StopMovement();

        if (animated && centreDuration > 0f)
        {
            centreCoroutine = StartCoroutine(
                AnimateCentre(targetNormalized)
            );
        }
        else
        {
            scrollRect.horizontalNormalizedPosition =
                targetNormalized;
        }
    }

    private IEnumerator AnimateCentre(float target)
    {
        float start = scrollRect.horizontalNormalizedPosition;
        float elapsed = 0f;

        while (elapsed < centreDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / centreDuration
            );

            t = Mathf.SmoothStep(0f, 1f, t);

            scrollRect.horizontalNormalizedPosition =
                Mathf.Lerp(start, target, t);

            yield return null;
        }

        scrollRect.horizontalNormalizedPosition = target;
        centreCoroutine = null;
    }

    private void OnDestroy()
    {
        foreach (Sprite sprite in sprites.Values)
        {
            if (sprite == null)
                continue;

            Texture2D texture = sprite.texture;

            Destroy(sprite);

            if (texture != null)
                Destroy(texture);
        }
    }
}
