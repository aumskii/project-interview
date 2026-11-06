using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class VisualManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image characterImage;
    [SerializeField] private Color fallbackBackground = new Color32(20, 25, 35, 255);

    public void Configure(Image canvasBackground, Image character)
    {
        backgroundImage = canvasBackground;
        characterImage = character;
    }

    public void SetBackground(string resourcePath)
    {
        Sprite sprite = LoadSprite(resourcePath, "Backgrounds");

        if (backgroundRenderer != null)
        {
            backgroundRenderer.sprite = sprite;
            backgroundRenderer.enabled = sprite != null;
        }

        if (backgroundImage != null)
        {
            backgroundImage.sprite = sprite;
            backgroundImage.color = sprite != null ? Color.white : fallbackBackground;
            backgroundImage.enabled = !string.IsNullOrWhiteSpace(resourcePath);
        }
    }

    public void SetCharacterSprite(string resourcePath)
    {
        if (characterImage == null)
            return;

        characterImage.sprite = LoadSprite(resourcePath, "Characters");
        characterImage.enabled = characterImage.sprite != null;
    }

    private static Sprite LoadSprite(string resourcePath, string folder)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return null;

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null || resourcePath.Contains("/"))
            return sprite;

        return Resources.Load<Sprite>($"{folder}/{resourcePath}");
    }
}
