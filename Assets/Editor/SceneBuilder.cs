using System;
using System.Collections.Generic;
using System.IO;
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
    private const byte VisibleAlphaThreshold = 16;

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
        VisualManager.CharacterTrim[] characterTrims = ReadCharacterTrims();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CameraShake cameraShake = CreateCamera();
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
        cameraShake.Configure(background.rectTransform);

        Image character = CreateImage(canvas, "Character", Color.white);
        RectTransform characterRect = character.rectTransform;
        var bottomCenter = new Vector2(0.5f, 0f);
        characterRect.anchorMin = bottomCenter;
        characterRect.anchorMax = bottomCenter;
        characterRect.pivot = bottomCenter;
        characterRect.anchoredPosition = Vector2.zero;
        characterRect.sizeDelta = scaler.referenceResolution;
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

        Button back = CreateHistoryButton(panel.rectTransform, "Back Button", false);
        Button forward = CreateHistoryButton(panel.rectTransform, "Forward Button", true);

        Image continueIndicator = CreateImage(panel.rectTransform, "Continue Indicator", Color.white);
        RectTransform indicatorRect = continueIndicator.rectTransform;
        indicatorRect.anchorMin = Vector2.right;
        indicatorRect.anchorMax = Vector2.right;
        indicatorRect.anchoredPosition = new Vector2(-104f, 27f);
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

        Button choiceTemplate = CreateChoiceButton(canvas, "Choice Button Template", font);
        var choiceSize = choiceTemplate.gameObject.AddComponent<LayoutElement>();
        choiceSize.minHeight = 48f;
        choiceSize.preferredHeight = 66f;
        choiceTemplate.gameObject.SetActive(false);

        Image elevatorImage = CreateImage(canvas, "GoToElevatorButton", new Color(0.03f, 0.045f, 0.07f, 0.7f));
        elevatorImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        elevatorImage.type = Image.Type.Sliced;
        elevatorImage.pixelsPerUnitMultiplier = 2f;
        elevatorImage.raycastTarget = true;
        RectTransform elevatorRect = elevatorImage.rectTransform;
        elevatorRect.anchorMin = elevatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        elevatorRect.anchoredPosition = Vector2.zero;
        elevatorRect.sizeDelta = new Vector2(400f, 100f);
        var goToElevator = elevatorImage.gameObject.AddComponent<Button>();
        goToElevator.targetGraphic = elevatorImage;
        goToElevator.navigation = new Navigation { mode = Navigation.Mode.None };
        Image elevatorBorder = CreateImage(elevatorRect, "Outline", new Color(1f, 1f, 1f, 0.65f));
        elevatorBorder.sprite = elevatorImage.sprite;
        elevatorBorder.type = Image.Type.Sliced;
        elevatorBorder.pixelsPerUnitMultiplier = 2f;
        elevatorBorder.fillCenter = false;
        SetRect(elevatorBorder.rectTransform, 0f, 0f, 1f, 1f);
        var elevatorOutline = elevatorBorder.gameObject.AddComponent<Outline>();
        elevatorOutline.effectColor = new Color(1f, 1f, 1f, 0.65f);
        elevatorOutline.effectDistance = new Vector2(0.5f, -0.5f);
        elevatorOutline.useGraphicAlpha = false;
        TextMeshProUGUI elevatorLabel = CreateText(elevatorRect, "Label", font, 40f);
        elevatorLabel.text = "Иди к лифту";
        elevatorLabel.alignment = TextAlignmentOptions.Center;
        SetRect(elevatorLabel.rectTransform, 0f, 0f, 1f, 1f);
        goToElevator.gameObject.SetActive(false);

        Image wetSignImage = CreateImage(canvas, "WetSignButton", new Color32(80, 80, 80, 255));
        wetSignImage.sprite = Resources.Load<Sprite>("Backgrounds/ui_wet_floor_sign");
        wetSignImage.preserveAspect = true;
        wetSignImage.raycastTarget = true;
        RectTransform wetSignRect = wetSignImage.rectTransform;
        wetSignRect.anchorMin = wetSignRect.anchorMax = Vector2.zero;
        wetSignRect.pivot = new Vector2(0.5f, 0f);
        wetSignRect.anchoredPosition = new Vector2(250f, 50f);
        wetSignRect.sizeDelta = new Vector2(120f, 160f);
        var wetSign = wetSignImage.gameObject.AddComponent<Button>();
        wetSign.targetGraphic = wetSignImage;
        wetSign.transition = Selectable.Transition.None;
        wetSign.navigation = new Navigation { mode = Navigation.Mode.None };
        wetSign.interactable = false;
        wetSign.gameObject.SetActive(false);

        Image baton = CreateImage(canvas, "Baton Impact", Color.white);
        baton.preserveAspect = true;
        baton.gameObject.SetActive(false);

        RectTransform gameOverPanel = CreateRect(canvas, "GameOverPanel");
        SetRect(gameOverPanel, 0f, 0f, 1f, 1f);

        Image blackout = CreateImage(gameOverPanel, "Blackout", Color.black);
        blackout.raycastTarget = true;
        SetRect(blackout.rectTransform, 0f, 0f, 1f, 1f);
        var blackoutFade = blackout.gameObject.AddComponent<CanvasGroup>();
        blackoutFade.alpha = 0f;

        TextMeshProUGUI gameOverTitle = CreateText(gameOverPanel, "Game Over Title", font, 136f);
        gameOverTitle.text = "ВАС ВЫГНАЛИ";
        gameOverTitle.color = Color.white;
        gameOverTitle.fontStyle = FontStyles.Bold;
        gameOverTitle.alignment = TextAlignmentOptions.Center;
        SetRect(gameOverTitle.rectTransform, 0.04f, 0.36f, 0.96f, 0.64f);
        var titleFade = gameOverTitle.gameObject.AddComponent<CanvasGroup>();
        titleFade.alpha = 0f;

        Button retry = CreateChoiceButton(gameOverPanel, "Retry Button", font);
        retry.GetComponentInChildren<TMP_Text>().text = "Попробовать снова";
        SetRect((RectTransform)retry.transform, 0.30f, 0.105f, 0.70f, 0.18f);
        retry.interactable = false;
        var retryFade = retry.gameObject.AddComponent<CanvasGroup>();
        retryFade.alpha = 0f;
        retryFade.blocksRaycasts = false;
        gameOverPanel.gameObject.SetActive(false);

        Image creditsPanel = CreateImage(canvas, "CreditsPanel", Color.black);
        creditsPanel.raycastTarget = true;
        SetRect(creditsPanel.rectTransform, 0f, 0f, 1f, 1f);
        creditsPanel.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI endingText = CreateText(creditsPanel.rectTransform, "Ending Text", font, 72f);
        endingText.fontSizeMin = 48f;
        endingText.alignment = TextAlignmentOptions.Center;
        SetRect(endingText.rectTransform, 0.10f, 0.28f, 0.90f, 0.72f);
        var endingTextFade = endingText.gameObject.AddComponent<CanvasGroup>();

        TextMeshProUGUI creditsText = CreateText(creditsPanel.rectTransform, "Scrolling Credits", font, 48f);
        creditsText.enableAutoSizing = false;
        creditsText.alignment = TextAlignmentOptions.Top;
        creditsText.lineSpacing = 20f;
        RectTransform creditsRect = creditsText.rectTransform;
        creditsRect.anchorMin = creditsRect.anchorMax = new Vector2(0.5f, 0f);
        creditsRect.pivot = new Vector2(0.5f, 1f);
        creditsRect.sizeDelta = new Vector2(1536f, 1080f);
        creditsRect.anchoredPosition = new Vector2(0f, -40f);
        creditsText.gameObject.SetActive(false);
        Button creditsBack = CreateHistoryButton(creditsPanel.rectTransform, "Credits Back Button", false);
        ((RectTransform)creditsBack.transform).anchoredPosition = new Vector2(135f, 60f);
        creditsPanel.gameObject.SetActive(false);

        var systems = new GameObject("VN Systems");
        systems.AddComponent<VisualManager>().Configure(background, character, characterTrims);
        var manager = systems.AddComponent<DialogueManager>();
        manager.Configure(panel.gameObject, speaker, dialogue, choices, choiceTemplate,
            advanceArea, continueIndicator, back, forward);
        manager.ConfigureGameOver(gameOverPanel.gameObject, baton, blackoutFade, titleFade, retry, retryFade, cameraShake);
        manager.ConfigureMusic(CreateMusicSource(systems.transform, "MusicNeutral"),
            CreateMusicSource(systems.transform, "MusicTense"), CreateMusicSource(systems.transform, "MusicHorror"));
        manager.ConfigureFinale(wetSign, creditsPanel.gameObject, endingText, endingTextFade,
            creditsText, characterRect, goToElevator, creditsBack);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError($"The VN scene could not be saved to {ScenePath}.");
            return;
        }

        Selection.activeGameObject = systems;
        Debug.Log($"VN scene saved to {ScenePath}.", systems);
    }

    private static VisualManager.CharacterTrim[] ReadCharacterTrims()
    {
        TextAsset json = Resources.Load<TextAsset>(DialogueManager.DialoguePath);
        if (json == null)
            return Array.Empty<VisualManager.CharacterTrim>();

        GameDataContainer data;
        try
        {
            data = JsonUtility.FromJson<GameDataContainer>(json.text);
        }
        catch (ArgumentException)
        {
            return Array.Empty<VisualManager.CharacterTrim>();
        }

        var trims = new List<VisualManager.CharacterTrim>();
        var checkedSprites = new HashSet<Sprite>();
        foreach (DialogueNode node in data?.nodes ?? Array.Empty<DialogueNode>())
        {
            Sprite sprite = VisualManager.LoadCharacterSprite(node?.characterSprite);
            if (sprite == null || !checkedSprites.Add(sprite))
                continue;

            string path = AssetDatabase.GetAssetPath(sprite);
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                !(AssetImporter.GetAtPath(path) is TextureImporter importer) ||
                importer.spriteImportMode != SpriteImportMode.Single)
                continue;

            float bottomFraction = ReadBottomPadding(path);
            if (bottomFraction > 0f)
                trims.Add(new VisualManager.CharacterTrim { sprite = sprite, bottomFraction = bottomFraction });
        }

        return trims.ToArray();
    }

    private static float ReadBottomPadding(string path)
    {
        var texture = new Texture2D(2, 2);
        try
        {
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path)))
                return 0f;

            Color32[] pixels = texture.GetPixels32();
            int width = texture.width;
            for (int y = 0; y < texture.height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[row + x].a >= VisibleAlphaThreshold)
                        return (float)y / texture.height;
                }
            }

            return 0f;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static AudioSource CreateMusicSource(Transform parent, string name)
    {
        var source = new GameObject(name, typeof(AudioSource)).GetComponent<AudioSource>();
        source.transform.SetParent(parent, false);
        source.playOnAwake = false;
        source.loop = true;
        return source;
    }

    private static CameraShake CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener),
            typeof(CameraShake));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        return cameraObject.GetComponent<CameraShake>();
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

    private static Button CreateHistoryButton(Transform parent, string name, bool onRight)
    {
        Image hitArea = CreateImage(parent, name, Color.clear);
        hitArea.raycastTarget = true;
        RectTransform rect = hitArea.rectTransform;
        rect.anchorMin = rect.anchorMax = onRight ? Vector2.right : Vector2.zero;
        rect.anchoredPosition = new Vector2(onRight ? -49f : 49f, 27f);
        rect.sizeDelta = new Vector2(66f, 44f);

        var arrow = CreateRect(rect, "Arrow").gameObject.AddComponent<HistoryArrowGraphic>();
        arrow.raycastTarget = false;
        arrow.rectTransform.sizeDelta = new Vector2(20f, 22f);
        arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, onRight ? 0f : 180f);

        var button = hitArea.gameObject.AddComponent<Button>();
        button.targetGraphic = arrow;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color32(237, 214, 162, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.24f);
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        button.interactable = false;
        return button;
    }

    private static Button CreateChoiceButton(Transform parent, string name, TMP_FontAsset font)
    {
        Image image = CreateImage(parent, name, Color.white);
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
