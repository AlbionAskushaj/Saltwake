// Builds Saltwake's title screen scene programmatically.
//
// Run from the Unity menu:  Tools > Saltwake > Build Title Screen
//
// Creates Assets/TitleScreen/TitleScreen.unity with:
//   - Dark-blue camera background
//   - SALTWAKE title text + subtitle
//   - Start Game and Quit buttons (wired to TitleScreen.cs)
//   - Credit line for the team
// Then registers the scene at build index 0 (and shifts existing scenes down).
//
// Safe to re-run: it overwrites the existing scene file.

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class TitleScreenBuilder
{
    const string SCENE_PATH = "Assets/TitleScreen/TitleScreen.unity";

    static readonly Color BgColor       = new Color(0.04f, 0.07f, 0.13f, 1f); // deep navy
    static readonly Color TitleColor    = new Color(0.86f, 0.95f, 1.00f, 1f); // pale ice
    static readonly Color SubtitleColor = new Color(0.55f, 0.72f, 0.85f, 1f); // muted blue
    static readonly Color ButtonColor   = new Color(0.10f, 0.18f, 0.28f, 1f);
    static readonly Color ButtonHover   = new Color(0.18f, 0.32f, 0.48f, 1f);
    static readonly Color CreditColor   = new Color(0.55f, 0.65f, 0.75f, 0.8f);

    [MenuItem("Tools/Saltwake/Build Title Screen")]
    public static void Build()
    {
        // 1. Create a new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Camera with dark background
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BgColor;
        cam.orthographic = true;
        camGO.AddComponent<AudioListener>();

        // 3. EventSystem (required for UI input)
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // 4. Canvas
        var canvasGO = new GameObject("UI_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // 5. TitleScreen GameObject (holds the controller script)
        var controllerGO = new GameObject("TitleScreenController");
        var controller = controllerGO.AddComponent<TitleScreen>();

        // 6. UI children
        var font = TMP_Settings.defaultFontAsset;

        // Background panel (subtle vignette over the camera color)
        var bgPanel = CreateUIElement("Background", canvasGO.transform);
        var bgImg = bgPanel.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.35f);
        StretchToParent(bgPanel.GetComponent<RectTransform>());

        // Title text
        var titleGO = CreateUIElement("Title", canvasGO.transform);
        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "SALTWAKE";
        title.font = font;
        title.fontSize = 180;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = TitleColor;
        title.characterSpacing = 20f;
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.5f);
        titleRT.anchorMax = new Vector2(0.5f, 0.5f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.anchoredPosition = new Vector2(0, 240);
        titleRT.sizeDelta = new Vector2(1600, 220);

        // Subtitle
        var subGO = CreateUIElement("Subtitle", canvasGO.transform);
        var sub = subGO.AddComponent<TextMeshProUGUI>();
        sub.text = "A 2D Action-Platformer";
        sub.font = font;
        sub.fontSize = 48;
        sub.fontStyle = FontStyles.Italic;
        sub.alignment = TextAlignmentOptions.Center;
        sub.color = SubtitleColor;
        var subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.5f);
        subRT.anchorMax = new Vector2(0.5f, 0.5f);
        subRT.pivot = new Vector2(0.5f, 0.5f);
        subRT.anchoredPosition = new Vector2(0, 100);
        subRT.sizeDelta = new Vector2(1200, 80);

        // Start button
        var startBtnGO = CreateButton("StartButton", canvasGO.transform, "START GAME", font);
        var startRT = startBtnGO.GetComponent<RectTransform>();
        startRT.anchoredPosition = new Vector2(0, -80);
        var startBtn = startBtnGO.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(startBtn.onClick, controller.StartGame);

        // Quit button
        var quitBtnGO = CreateButton("QuitButton", canvasGO.transform, "QUIT", font);
        var quitRT = quitBtnGO.GetComponent<RectTransform>();
        quitRT.anchoredPosition = new Vector2(0, -200);
        var quitBtn = quitBtnGO.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(quitBtn.onClick, controller.QuitGame);

        // Credit line at bottom
        var creditGO = CreateUIElement("Credit", canvasGO.transform);
        var credit = creditGO.AddComponent<TextMeshProUGUI>();
        credit.text = "Game Design 4483";
        credit.font = font;
        credit.fontSize = 28;
        credit.alignment = TextAlignmentOptions.Center;
        credit.color = CreditColor;
        var creditRT = creditGO.GetComponent<RectTransform>();
        creditRT.anchorMin = new Vector2(0.5f, 0f);
        creditRT.anchorMax = new Vector2(0.5f, 0f);
        creditRT.pivot = new Vector2(0.5f, 0f);
        creditRT.anchoredPosition = new Vector2(0, 60);
        creditRT.sizeDelta = new Vector2(1400, 50);

        // Hint text under buttons
        var hintGO = CreateUIElement("Hint", canvasGO.transform);
        var hint = hintGO.AddComponent<TextMeshProUGUI>();
        hint.text = "Press ENTER to start  ·  ESC to quit";
        hint.font = font;
        hint.fontSize = 24;
        hint.fontStyle = FontStyles.Italic;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(0.55f, 0.65f, 0.75f, 0.6f);
        var hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0.5f, 0f);
        hintRT.anchorMax = new Vector2(0.5f, 0f);
        hintRT.pivot = new Vector2(0.5f, 0f);
        hintRT.anchoredPosition = new Vector2(0, 140);
        hintRT.sizeDelta = new Vector2(1400, 40);

        // 7. Save scene
        System.IO.Directory.CreateDirectory("Assets/TitleScreen");
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AssetDatabase.Refresh();

        // 8. Add to Build Settings at index 0 (shifts existing scenes down)
        AddSceneToBuildSettingsAtIndex0(SCENE_PATH);

        Debug.Log("Title screen built and saved to " + SCENE_PATH +
                  ". Build Settings updated: TitleScreen=0, Level1=1, Level2=2, Level3=3.");
    }

    static GameObject CreateUIElement(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static GameObject CreateButton(string name, Transform parent, string label, TMP_FontAsset font)
    {
        var btnGO = CreateUIElement(name, parent);
        var img = btnGO.AddComponent<Image>();
        img.color = ButtonColor;
        var btn = btnGO.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHover;
        colors.pressedColor = new Color(0.06f, 0.12f, 0.20f, 1f);
        colors.selectedColor = ButtonHover;
        btn.colors = colors;

        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(420, 90);

        // Label
        var labelGO = CreateUIElement("Label", btnGO.transform);
        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.font = font;
        labelText.fontSize = 40;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = TitleColor;
        StretchToParent(labelGO.GetComponent<RectTransform>());

        return btnGO;
    }

    static void AddSceneToBuildSettingsAtIndex0(string scenePath)
    {
        var current = EditorBuildSettings.scenes;

        // Remove any existing entry for this scene (avoid duplicates on re-run)
        var filtered = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var s in current)
        {
            if (s.path != scenePath)
                filtered.Add(s);
        }

        var newScenes = new EditorBuildSettingsScene[filtered.Count + 1];
        newScenes[0] = new EditorBuildSettingsScene(scenePath, true);
        for (int i = 0; i < filtered.Count; i++)
            newScenes[i + 1] = filtered[i];

        EditorBuildSettings.scenes = newScenes;
    }
}
