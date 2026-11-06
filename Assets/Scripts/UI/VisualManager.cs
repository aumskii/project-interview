using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class VisualManager : MonoBehaviour
{
    [Serializable]
    public struct CharacterTrim
    {
        public Sprite sprite;
        public float bottomFraction;
    }

    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image characterImage;
    [SerializeField] private Color fallbackBackground = new Color32(20, 25, 35, 255);
    [SerializeField] private CharacterTrim[] characterTrims = Array.Empty<CharacterTrim>();

    private readonly Dictionary<Sprite, Sprite> alignedCharacters = new Dictionary<Sprite, Sprite>();

    public void Configure(Image canvasBackground, Image character, CharacterTrim[] trims)
    {
        backgroundImage = canvasBackground;
        characterImage = character;
        characterTrims = trims;
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
            backgroundImage.preserveAspect = sprite != null && sprite.rect.height > sprite.rect.width;
            backgroundImage.color = sprite != null ? Color.white : fallbackBackground;
            backgroundImage.enabled = !string.IsNullOrWhiteSpace(resourcePath);
        }
    }

    public void SetCharacterSprite(string resourcePath)
    {
        if (characterImage == null)
            return;

        characterImage.sprite = GetBottomAlignedSprite(LoadCharacterSprite(resourcePath));
        characterImage.enabled = characterImage.sprite != null;
    }

    public static Sprite LoadCharacterSprite(string resourcePath) => LoadSprite(resourcePath, "Characters");

    private Sprite GetBottomAlignedSprite(Sprite source)
    {
        if (source == null || source.packed)
            return source;

        if (alignedCharacters.TryGetValue(source, out Sprite aligned))
            return aligned;

        foreach (CharacterTrim trim in characterTrims)
        {
            if (trim.sprite != source)
                continue;

            Rect rect = source.rect;
            float padding = Mathf.Round(rect.height * trim.bottomFraction);
            if (padding <= 0f || padding >= rect.height)
                return source;

            rect.yMin += padding;
            aligned = Sprite.Create(source.texture, rect, new Vector2(0.5f, 0f), source.pixelsPerUnit,
                0, SpriteMeshType.FullRect);
            aligned.name = source.name;
            alignedCharacters.Add(source, aligned);
            return aligned;
        }

        return source;
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

    private void OnDestroy()
    {
        foreach (Sprite sprite in alignedCharacters.Values)
            Destroy(sprite);
    }
}
