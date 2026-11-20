using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuBuilder
{
    private const string MenuPath = "Tools/Build Main Menu";
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem(MenuPath, true)]
    private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem(MenuPath)]
    public static void Build()
    {
        if (TMP_Settings.LoadDefaultSettings() == null || TMP_Settings.defaultFontAsset == null)
        {
            Debug.LogError("Import TMP Essential Resources before building the main menu.");
            return;
        }

        Sprite officeEntrance = Resources.Load<Sprite>("Backgrounds/bg_office_entrance");
        if (officeEntrance == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneBuilder.ScenePath) == null)
        {
            Debug.LogError("The main menu requires the bg_office_entrance sprite and Assets/VNScene.unity.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;

        EventSystem events = SceneBuilder.CreateEventSystem();
        var canvas = (RectTransform)SceneBuilder.CreateCanvas("Main Menu Canvas").transform;
        Image background = SceneBuilder.CreateImage(canvas, "Office Entrance", Color.white);
        background.sprite = officeEntrance;
        SceneBuilder.SetRect(background.rectTransform, 0f, 0f, 1f, 1f);
        var backgroundFit = background.gameObject.AddComponent<AspectRatioFitter>();
        backgroundFit.aspectRatio = officeEntrance.rect.width / officeEntrance.rect.height;
        backgroundFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

        Image dimmer = SceneBuilder.CreateImage(canvas, "Background Dimmer", new Color(0f, 0f, 0f, 0.5f));
        SceneBuilder.SetRect(dimmer.rectTransform, 0f, 0f, 1f, 1f);

        TextMeshProUGUI title = SceneBuilder.CreateText(canvas, "Title", font, 120f);
        title.text = "СОБЕСЕДОВАНИЕ";
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        SceneBuilder.SetRect(title.rectTransform, 0.07f, 0.745f, 0.93f, 0.91f);

        TextMeshProUGUI subtitle = SceneBuilder.CreateText(canvas, "Subtitle", font, 36f);
        subtitle.text = "Пройди путь от ресепшена до кабинета директора. Постарайся выжить.";
        subtitle.color = new Color32(216, 218, 222, 255);
        subtitle.alignment = TextAlignmentOptions.Center;
        SceneBuilder.SetRect(subtitle.rectTransform, 0.1f, 0.635f, 0.9f, 0.745f);

        var controller = new GameObject("Main Menu Controller").AddComponent<MainMenuController>();
        Button start = CreateButton(canvas, "Start Game", "Начать собеседование", font, 0.515f, 0.595f);
        Button exit = CreateButton(canvas, "Exit Game", "Выход", font, 0.405f, 0.485f);
        UnityEventTools.AddPersistentListener(start.onClick, controller.OnStartGame);
        UnityEventTools.AddPersistentListener(exit.onClick, controller.OnExitGame);
        events.firstSelectedGameObject = start.gameObject;

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError($"The main menu scene could not be saved to {ScenePath}.");
            return;
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(SceneBuilder.ScenePath, true),
        };
        Debug.Log($"Main menu saved to {ScenePath} and added before VNScene in Build Settings.");
    }

    private static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font,
        float bottom, float top)
    {
        Image image = SceneBuilder.CreateImage(parent, name, Color.white);
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.raycastTarget = true;
        SceneBuilder.SetRect(image.rectTransform, 0.36f, bottom, 0.64f, top);

        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color32(17, 25, 36, 235);
        colors.highlightedColor = new Color32(38, 54, 72, 255);
        colors.selectedColor = new Color32(31, 44, 60, 248);
        colors.pressedColor = new Color32(11, 17, 26, 255);
        colors.fadeDuration = 0.15f;
        button.colors = colors;

        TextMeshProUGUI text = SceneBuilder.CreateText(image.rectTransform, "Label", font, 36f);
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.margin = new Vector4(20f, 0f, 20f, 0f);
        SceneBuilder.SetRect(text.rectTransform, 0f, 0f, 1f, 1f);
        return button;
    }
}
