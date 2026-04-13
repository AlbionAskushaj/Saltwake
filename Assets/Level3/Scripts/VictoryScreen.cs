using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class VictoryScreen : MonoBehaviour
{
    private static VictoryScreen instance;
    private bool waitingForInput;

    public static void Show()
    {
        if (instance != null) return;

        GameObject go = new GameObject("VictoryScreen");
        instance = go.AddComponent<VictoryScreen>();
    }

    private void Start()
    {
        StartCoroutine(ShowVictorySequence());
    }

    private IEnumerator ShowVictorySequence()
    {
        // --- Canvas ---
        GameObject canvasGO = new GameObject("VictoryCanvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // --- Dark overlay ---
        GameObject panelGO = new GameObject("Overlay");
        panelGO.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0);

        // Fade overlay in
        float elapsed = 0f;
        float fadeDuration = 2.5f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            panelImage.color = new Color(0, 0, 0, Mathf.Clamp01(elapsed / fadeDuration) * 0.9f);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.6f);

        // --- Title ---
        TextMeshProUGUI titleText = CreateText(canvasGO.transform, "Title",
            new Vector2(0.5f, 0.62f), new Vector2(900, 120),
            "CONGRATULATIONS", 72,
            new Color(0.92f, 0.78f, 0.35f));
        titleText.fontStyle = FontStyles.Bold;

        // Fade title in
        titleText.alpha = 0;
        elapsed = 0f;
        while (elapsed < 1.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            titleText.alpha = Mathf.Clamp01(elapsed / 1.5f);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.4f);

        // --- Subtitle ---
        TextMeshProUGUI subText = CreateText(canvasGO.transform, "Subtitle",
            new Vector2(0.5f, 0.48f), new Vector2(900, 80),
            "You have conquered the depths of Saltwake.", 36,
            new Color(0.8f, 0.88f, 0.95f));

        subText.alpha = 0;
        elapsed = 0f;
        while (elapsed < 1.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            subText.alpha = Mathf.Clamp01(elapsed / 1.5f);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.3f);

        // --- Thanks ---
        TextMeshProUGUI thanksText = CreateText(canvasGO.transform, "Thanks",
            new Vector2(0.5f, 0.38f), new Vector2(900, 60),
            "Thanks for playing!", 28,
            Color.white);

        thanksText.alpha = 0;
        elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.unscaledDeltaTime;
            thanksText.alpha = Mathf.Clamp01(elapsed / 1f);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(1.5f);

        // --- Prompt ---
        TextMeshProUGUI promptText = CreateText(canvasGO.transform, "Prompt",
            new Vector2(0.5f, 0.22f), new Vector2(900, 50),
            "Press any key to return to the menu", 22,
            new Color(0.6f, 0.6f, 0.6f));

        promptText.alpha = 0;
        elapsed = 0f;
        while (elapsed < 0.8f)
        {
            elapsed += Time.unscaledDeltaTime;
            promptText.alpha = Mathf.Clamp01(elapsed / 0.8f);
            yield return null;
        }

        Time.timeScale = 0f;
        waitingForInput = true;
    }

    private void Update()
    {
        if (!waitingForInput) return;

        if (Input.anyKeyDown)
        {
            waitingForInput = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        instance = null;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name,
        Vector2 anchorPos, Vector2 size, string content, float fontSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        return tmp;
    }
}
