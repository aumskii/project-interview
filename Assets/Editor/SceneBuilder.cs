using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

public static class SceneBuilder
{
    private const string MenuPath = "Tools/Build VN Scene";
    private const string ScenePath = "Assets/VNScene.unity";

    [MenuItem(MenuPath, true)]
    private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem(MenuPath)]
    public static void Build()
    {
        if (TMP_Settings.LoadDefaultSettings() == null || TMP_Settings.defaultFontAsset == null)
        {
            Debug.LogError("Import TMP Essential Resources via Window > TextMeshPro before building the VN scene.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        CreateEventSystem();

        var canvasObject = new GameObject("VN Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var canvas = (RectTransform)canvasObject.transform;

        Image background = CreateImage(canvas, "Background", Color.black);
        SetRect(background.rectTransform, 0f, 0f, 1f, 1f);

        Image character = CreateImage(canvas, "Character", Color.white);
        RectTransform characterRect = character.rectTransform;
        var bottomCenter = new Vector2(0.5f, 0f);
        characterRect.anchorMin = bottomCenter;
        characterRect.anchorMax = bottomCenter;
        characterRect.pivot = bottomCenter;
        characterRect.anchoredPosition = Vector2.zero;
        characterRect.sizeDelta = new Vector2(940f, 1000f);
        character.preserveAspect = true;
        character.enabled = false;

        Button advanceArea = CreateAdvanceArea(canvas);

        Image panel = CreateImage(canvas, "Dialogue Panel", new Color(0f, 0f, 0f, 0.65f));
        SetRect(panel.rectTransform, 0.045f, 0.03f, 0.955f, 0.285f);

        TextMeshProUGUI speaker = CreateText(panel.rectTransform, "Speaker", font, 32f);
        speaker.color = new Color32(237, 214, 162, 255);
        speaker.alignment = TextAlignmentOptions.Left;
        SetRect(speaker.rectTransform, 0.03f, 0.74f, 0.97f, 0.94f);

        TextMeshProUGUI dialogue = CreateText(panel.rectTransform, "Dialogue", font, 38f);
        dialogue.alignment = TextAlignmentOptions.TopLeft;
        SetRect(dialogue.rectTransform, 0.03f, 0.17f, 0.97f, 0.74f);

        Image continueIndicator = CreateImage(panel.rectTransform, "Continue Indicator", Color.white);
        RectTransform indicatorRect = continueIndicator.rectTransform;
        indicatorRect.anchorMin = Vector2.right;
        indicatorRect.anchorMax = Vector2.right;
        indicatorRect.anchoredPosition = new Vector2(-44f, 26f);
        indicatorRect.sizeDelta = new Vector2(13f, 13f);
        indicatorRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        continueIndicator.gameObject.SetActive(false);

        RectTransform choices = CreateRect(canvas, "Choices");
        SetRect(choices, 0.13f, 0.305f, 0.87f, 0.72f);
        var layout = choices.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Button choiceTemplate = CreateChoiceButton(canvas, font);
        var choiceSize = choiceTemplate.gameObject.AddComponent<LayoutElement>();
        choiceSize.minHeight = 48f;
        choiceSize.preferredHeight = 66f;
        choiceTemplate.gameObject.SetActive(false);

        var systems = new GameObject("VN Systems");
        systems.AddComponent<VisualManager>().Configure(background, character);
        systems.AddComponent<DialogueManager>().Configure(speaker, dialogue, choices, choiceTemplate,
            advanceArea, continueIndicator);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError($"The VN scene could not be saved to {ScenePath}.");
            return;
        }

        Selection.activeGameObject = systems;
        Debug.Log($"VN scene saved to {ScenePath}.", systems);
    }

    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener),
            typeof(CameraShake));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
    }

    private static void CreateEventSystem()
    {
        var events = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        events.AddComponent<InputSystemUIInputModule>();
#else
        events.AddComponent<StandaloneInputModule>();
#endif
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        var image = CreateRect(parent, name).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, TMP_FontAsset font, float size)
    {
        var text = CreateRect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = string.Empty;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = 22f;
        text.fontSizeMax = size;
        text.color = Color.white;
        text.raycastTarget = false;
        text.richText = false;
        text.parseCtrlCharacters = false;
        return text;
    }

    private static Button CreateAdvanceArea(Transform parent)
    {
        Image image = CreateImage(parent, "Advance Area", Color.clear);
        image.raycastTarget = true;
        SetRect(image.rectTransform, 0f, 0f, 1f, 1f);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.gameObject.SetActive(false);
        return button;
    }

    private static Button CreateChoiceButton(Transform parent, TMP_FontAsset font)
    {
        Image image = CreateImage(parent, "Choice Button Template", Color.white);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0f, 0f, 0f, 0.7f);
        colors.highlightedColor = new Color(0.16f, 0.16f, 0.16f, 0.7f);
        colors.pressedColor = new Color(0.08f, 0.08f, 0.08f, 0.7f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0f, 0f, 0f, 0.45f);
        colors.fadeDuration = 0.12f;
        button.colors = colors;

        TextMeshProUGUI text = CreateText(image.rectTransform, "Label", font, 32f);
        text.color = new Color32(247, 245, 234, 255);
        text.alignment = TextAlignmentOptions.Center;
        text.margin = new Vector4(24f, 3f, 24f, 3f);
        SetRect(text.rectTransform, 0f, 0f, 1f, 1f);
        return button;
    }

    private static void SetRect(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
