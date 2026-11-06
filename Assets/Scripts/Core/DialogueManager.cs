using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(VisualManager))]
public sealed class DialogueManager : MonoBehaviour
{
    private const string DialoguePath = "Data/dialogue";
    private const float IndicatorPeriod = 1.2f;

    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private RectTransform choicesContainer;
    [SerializeField] private Button choiceButtonTemplate;
    [SerializeField] private Button advanceArea;
    [SerializeField] private Image continueIndicator;
    [SerializeField, Min(1f)] private float charactersPerSecond = 52f;

    private readonly Dictionary<string, DialogueNode> nodes =
        new Dictionary<string, DialogueNode>(StringComparer.Ordinal);
    private readonly List<Button> choiceButtons = new List<Button>();

    private VisualManager visuals;
    private DialogueNode currentNode;
    private int lineLength;
    private float visibleCharacters;
    private float indicatorStartedAt;

    public void Configure(TMP_Text speaker, TMP_Text line, RectTransform choices,
        Button choiceTemplate, Button advance, Image indicator)
    {
        speakerText = speaker;
        dialogueText = line;
        choicesContainer = choices;
        choiceButtonTemplate = choiceTemplate;
        advanceArea = advance;
        continueIndicator = indicator;
    }

    private void Start()
    {
        if (speakerText == null || dialogueText == null || choicesContainer == null ||
            advanceArea == null || continueIndicator == null || choiceButtonTemplate == null ||
            choiceButtonTemplate.GetComponentInChildren<TMP_Text>(true) == null)
        {
            Debug.LogError("Dialogue UI bindings are incomplete. Rebuild the scene via Tools > Build VN Scene.", this);
            enabled = false;
            return;
        }

        visuals = GetComponent<VisualManager>();
        SetContinuationVisible(false);
        choiceButtonTemplate.gameObject.SetActive(false);

        if (!TryLoadDialogue(out string entryNodeId))
        {
            enabled = false;
            return;
        }

        advanceArea.onClick.AddListener(Advance);
        ShowNode(entryNodeId);
    }

    private bool TryLoadDialogue(out string entryNodeId)
    {
        entryNodeId = null;
        TextAsset json = Resources.Load<TextAsset>(DialoguePath);
        if (json == null)
        {
            Debug.LogError($"Dialogue file was not found at Resources/{DialoguePath}.json.", this);
            return false;
        }

        GameDataContainer data;
        try
        {
            data = JsonUtility.FromJson<GameDataContainer>(json.text);
        }
        catch (ArgumentException exception)
        {
            Debug.LogError($"Invalid dialogue JSON. {exception.Message}", this);
            return false;
        }

        if (data?.nodes == null || data.nodes.Length == 0)
        {
            Debug.LogError("Dialogue JSON has no nodes.", this);
            return false;
        }

        foreach (DialogueNode node in data.nodes)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.id) || nodes.ContainsKey(node.id))
            {
                Debug.LogError($"Dialogue node IDs must be nonempty and unique. Invalid ID: '{node?.id}'.", this);
                nodes.Clear();
                return false;
            }

            nodes.Add(node.id, node);
        }

        entryNodeId = data.nodes[0].id;
        return true;
    }

    public void ShowNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || !nodes.TryGetValue(nodeId, out DialogueNode node))
        {
            Debug.LogWarning($"Dialogue node '{nodeId}' was not found.", this);
            return;
        }

        SetContinuationVisible(false);
        foreach (Button button in choiceButtons)
        {
            button.gameObject.SetActive(false);
            Destroy(button.gameObject);
        }
        choiceButtons.Clear();

        currentNode = node;
        visuals.SetBackground(node.background);
        visuals.SetCharacterSprite(node.characterSprite);
        speakerText.text = node.characterName ?? string.Empty;

        dialogueText.text = node.text ?? string.Empty;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        lineLength = dialogueText.textInfo.characterCount;
        visibleCharacters = 0f;

        if (lineLength == 0)
            ShowActions();
    }

    private void Update()
    {
        if (visibleCharacters < lineLength)
        {
            float speed = Mathf.Max(1f, charactersPerSecond);
            visibleCharacters = Mathf.Min(lineLength, visibleCharacters + Time.unscaledDeltaTime * speed);
            dialogueText.maxVisibleCharacters = (int)visibleCharacters;

            if (visibleCharacters >= lineLength)
                ShowActions();
            return;
        }

        if (!continueIndicator.isActiveAndEnabled)
            return;

        float phase = (Time.unscaledTime - indicatorStartedAt) * (2f * Mathf.PI / IndicatorPeriod);
        Color color = continueIndicator.color;
        color.a = (1f + Mathf.Cos(phase)) * 0.5f;
        continueIndicator.color = color;
    }

    private void ShowActions()
    {
        if (currentNode.choices == null || currentNode.choices.Length == 0)
        {
            SetContinuationVisible(HasTarget(currentNode.nextNodeId));
            return;
        }

        foreach (Choice choice in currentNode.choices)
        {
            if (choice == null)
                continue;

            Button button = Instantiate(choiceButtonTemplate, choicesContainer, false);
            button.GetComponentInChildren<TMP_Text>(true).text = choice.choiceText ?? string.Empty;
            button.interactable = HasTarget(choice.nextNodeId);
            if (button.interactable)
            {
                button.onClick.AddListener(() =>
                {
                    FlagsManager.Instance.SetFlag(choice.flagToSet);
                    ShowNode(choice.nextNodeId);
                });
            }

            choiceButtons.Add(button);
            button.gameObject.SetActive(true);
        }
    }

    private bool HasTarget(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
            return false;

        if (nodes.ContainsKey(targetId))
            return true;

        Debug.LogWarning($"Dialogue node '{currentNode.id}' refers to missing node '{targetId}'.", this);
        return false;
    }

    private void SetContinuationVisible(bool visible)
    {
        advanceArea.gameObject.SetActive(visible);
        continueIndicator.gameObject.SetActive(visible);
        if (visible)
        {
            indicatorStartedAt = Time.unscaledTime;
            continueIndicator.color = Color.white;
        }
    }

    private void Advance()
    {
        if (isActiveAndEnabled && advanceArea.isActiveAndEnabled && advanceArea.interactable)
            ShowNode(currentNode.nextNodeId);
    }

    private void OnDestroy()
    {
        if (advanceArea != null)
            advanceArea.onClick.RemoveListener(Advance);
    }
}
