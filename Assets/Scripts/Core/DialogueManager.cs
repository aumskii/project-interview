using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(VisualManager))]
public sealed class DialogueManager : MonoBehaviour
{
    public const string DialoguePath = "Data/dialogue";
    private const float IndicatorPeriod = 1.2f;
    private const float BatonTravelSeconds = 0.7f;
    private const float ImpactShakeSeconds = 0.5f;
    private const float PostImpactPauseSeconds = 1f;
    private const float BlackScreenPauseSeconds = 1f;
    private const float GameOverTitleFadeSeconds = 3f;
    private const float GameOverRetryDelaySeconds = 3f;
    private const float GameOverRetryFadeSeconds = 1.5f;
    private const float FallDurationSeconds = 1.75f;
    private const float FallCaptionSeconds = 3f;
    private const float ElevatorPulsePeriod = 1.5f;
    private const float EndingCharacterSeconds = 0.1f;
    private const float EndingReadPauseSeconds = 5f;
    private const float EndingTextFadeSeconds = 1f;
    private const float CreditsScrollSpeed = 65f;
    private const string DeathEndingText =
        "Даже когда кажется, что все проблемы позади, не забывайте смотреть под ноги.";
    private const int EduardMistakeLimit = 3;
    private const string EduardGameOverNodeId = "eduard_game_over";

    private static readonly string[] EduardBackgrounds =
    {
        "bg_eduard_office_neutral", "bg_eduard_office_interested",
        "bg_eduard_office_angry", "bg_eduard_office_demon",
    };
    private static readonly string[] EduardPortraits =
    {
        "eduard_neutral", "eduard_smiling", "eduard_angry", "eduard_demon",
    };

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private RectTransform choicesContainer;
    [SerializeField] private Button choiceButtonTemplate;
    [SerializeField] private Button advanceArea;
    [SerializeField] private Image continueIndicator;
    [SerializeField] private Button backButton;
    [SerializeField] private Button forwardButton;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Image batonImage;
    [SerializeField] private CanvasGroup blackoutFade;
    [SerializeField] private CanvasGroup gameOverTitleFade;
    [SerializeField] private Button retryButton;
    [SerializeField] private CanvasGroup retryFade;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private AudioSource musicNeutral;
    [SerializeField] private AudioSource musicTense;
    [SerializeField] private AudioSource musicHorror;
    [SerializeField] private Button wetSignButton;
    [SerializeField] private Button goToElevatorButton;
    [SerializeField] private Button creditsBackButton;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private TMP_Text endingText;
    [SerializeField] private CanvasGroup endingTextFade;
    [SerializeField] private TMP_Text creditsText;
    [SerializeField] private RectTransform characterRect;
    [SerializeField, TextArea(4, 20)] private string creditsContent =
        "ИГРА «СОБЕСЕДОВАНИЕ»\n\n---\n\n" +
        "Руководство компании: Эдуард Эдуардович\n\n" +
        "Служба безопасности: Толик\n\n" +
        "Отдел кадров: Инга\n\n" +
        "Соискатель: Слава";
    [SerializeField, Min(1f)] private float charactersPerSecond = 52f;

    private readonly Dictionary<string, DialogueNode> nodes =
        new Dictionary<string, DialogueNode>(StringComparer.Ordinal);
    private readonly List<Button> choiceButtons = new List<Button>();
    private readonly List<(DialogueNode Node, string ChoiceFlag, bool AddsMistake)> history =
        new List<(DialogueNode Node, string ChoiceFlag, bool AddsMistake)>();

    private VisualManager visuals;
    private AudioSource currentMusic;
    private Coroutine gameOverSequence;
    private Coroutine finaleSequence;
    private Vector2 characterRestPosition;
    private bool isFalling;
    private bool isEnding;
    private DialogueNode currentNode;
    private int historyIndex = -1;
    private int lineLength;
    private float visibleCharacters;
    private float indicatorStartedAt;
    private float? autoAdvanceAt;

    private bool CanGoForward => historyIndex < history.Count - 1;
    private bool IsRetryNode => !string.IsNullOrWhiteSpace(currentNode?.retryNodeId);

    public void Configure(GameObject panel, TMP_Text speaker, TMP_Text line, RectTransform choices,
        Button choiceTemplate, Button advance, Image indicator, Button back, Button forward)
    {
        dialoguePanel = panel;
        speakerText = speaker;
        dialogueText = line;
        choicesContainer = choices;
        choiceButtonTemplate = choiceTemplate;
        advanceArea = advance;
        continueIndicator = indicator;
        backButton = back;
        forwardButton = forward;
    }

    public void ConfigureGameOver(GameObject panel, Image baton, CanvasGroup blackout, CanvasGroup title,
        Button retry, CanvasGroup retryGroup, CameraShake shake)
    {
        gameOverPanel = panel;
        batonImage = baton;
        blackoutFade = blackout;
        gameOverTitleFade = title;
        retryButton = retry;
        retryFade = retryGroup;
        cameraShake = shake;
    }

    public void ConfigureMusic(AudioSource neutral, AudioSource tense, AudioSource horror)
    {
        musicNeutral = neutral;
        musicTense = tense;
        musicHorror = horror;
    }

    public void ConfigureFinale(Button wetSign, GameObject panel, TMP_Text title,
        CanvasGroup titleFade, TMP_Text credits, RectTransform character, Button goToElevator = null,
        Button creditsBack = null)
    {
        wetSignButton = wetSign;
        goToElevatorButton = goToElevator;
        creditsBackButton = creditsBack;
        creditsPanel = panel;
        endingText = title;
        endingTextFade = titleFade;
        creditsText = credits;
        characterRect = character;
    }

    private void Start()
    {
        if (dialoguePanel == null || speakerText == null || dialogueText == null || choicesContainer == null ||
            advanceArea == null || continueIndicator == null || choiceButtonTemplate == null ||
            backButton == null || forwardButton == null || gameOverPanel == null || batonImage == null ||
            blackoutFade == null || gameOverTitleFade == null || retryButton == null || retryFade == null ||
            cameraShake == null || wetSignButton == null || goToElevatorButton == null || creditsBackButton == null ||
            creditsPanel == null || endingText == null ||
            endingTextFade == null || creditsText == null || characterRect == null ||
            choiceButtonTemplate.GetComponentInChildren<TMP_Text>(true) == null)
        {
            Debug.LogError("Dialogue UI bindings are incomplete. Rebuild the scene via Tools > Build VN Scene.", this);
            enabled = false;
            return;
        }

        visuals = GetComponent<VisualManager>();
        characterRestPosition = characterRect.anchoredPosition;
        SetContinuationVisible(false);
        choiceButtonTemplate.gameObject.SetActive(false);

        if (!TryLoadDialogue(out string entryNodeId))
        {
            enabled = false;
            return;
        }

        advanceArea.onClick.AddListener(Advance);
        backButton.onClick.AddListener(GoBack);
        forwardButton.onClick.AddListener(GoForward);
        retryButton.onClick.AddListener(Retry);
        wetSignButton.onClick.AddListener(ChooseSecretEnding);
        goToElevatorButton.onClick.AddListener(GoToElevator);
        creditsBackButton.onClick.AddListener(ReturnToFinale);
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

    public void ShowNode(string nodeId) => VisitNode(nodeId, null);

    public void RestartFromNode(string nodeId)
    {
        int checkpointIndex = history.FindLastIndex(historyIndex, entry => entry.Node.id == nodeId);
        if (!TryResolveNode(nodeId, checkpointIndex, null, false, out DialogueNode node))
            return;

        historyIndex = checkpointIndex;
        DiscardForwardHistory();
        if (checkpointIndex >= 0 && history[checkpointIndex].Node == node)
            DisplayNode(node, showImmediately: false);
        else
            EnterNode(node, null, false);
    }

    private void VisitNode(string nodeId, string choiceFlag, bool addsMistake = false)
    {
        if (!TryResolveNode(nodeId, historyIndex, choiceFlag, addsMistake, out DialogueNode node))
            return;

        DiscardForwardHistory();
        EnterNode(node, choiceFlag, addsMistake);
    }

    private void EnterNode(DialogueNode node, string choiceFlag, bool addsMistake)
    {
        FlagsManager.Instance.AddChoiceFlag(choiceFlag);
        if (addsMistake)
            FlagsManager.Instance.AddMistake();

        history.Add((node, choiceFlag, addsMistake));
        historyIndex = history.Count - 1;
        DisplayNode(node, showImmediately: false);
    }

    private bool TryResolveNode(string nodeId, int lastKeptIndex, string choiceFlag, bool addsMistake, out DialogueNode node)
    {
        for (int remaining = nodes.Count; remaining > 0; remaining--)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || !nodes.TryGetValue(nodeId, out node))
            {
                Debug.LogWarning($"Dialogue node '{nodeId}' was not found.", this);
                node = null;
                return false;
            }

            if ((node.useEduardAtmosphere || node.id == "finale_1") && !node.isEduardReaction &&
                node.id != EduardGameOverNodeId && string.IsNullOrWhiteSpace(node.retryNodeId) &&
                MistakeCountAt(lastKeptIndex) + (addsMistake ? 1 : 0) >= EduardMistakeLimit)
            {
                nodeId = EduardGameOverNodeId;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(node.requiredFlag) && node.requiredFlag != choiceFlag &&
                !HasFlagAt(node.requiredFlag, lastKeptIndex))
            {
                nodeId = node.missingFlagNodeId;
                continue;
            }

            if (node.redirectToNextNode)
            {
                nodeId = node.nextNodeId;
                continue;
            }

            return true;
        }

        Debug.LogWarning($"Could not resolve dialogue node '{nodeId}'. Check the flag redirects.", this);
        node = null;
        return false;
    }

    private bool HasFlagAt(string flag, int lastKeptIndex)
    {
        int discardedChoices = 0;
        for (int i = lastKeptIndex + 1; i < history.Count; i++)
        {
            if (history[i].ChoiceFlag == flag)
                discardedChoices++;
        }

        return FlagsManager.Instance.HasFlagAfterRemovingChoices(flag, discardedChoices);
    }

    private void DiscardForwardHistory()
    {
        if (!CanGoForward)
            return;

        int firstDiscarded = historyIndex + 1;
        for (int i = firstDiscarded; i < history.Count; i++)
        {
            FlagsManager.Instance.RemoveChoiceFlag(history[i].ChoiceFlag);
            if (history[i].AddsMistake)
                FlagsManager.Instance.RemoveMistake();
        }

        history.RemoveRange(firstDiscarded, history.Count - firstDiscarded);
    }

    private int MistakeCountAt(int lastKeptIndex)
    {
        int count = FlagsManager.Instance.MistakeCount;
        for (int i = lastKeptIndex + 1; i < history.Count; i++)
        {
            if (history[i].AddsMistake)
                count--;
        }

        return count;
    }

    private void DisplayNode(DialogueNode node, bool showImmediately)
    {
        autoAdvanceAt = null;
        ResetFinalePresentation();
        if (IsRetryNode)
            ResetGameOverPresentation();

        SetContinuationVisible(false);
        foreach (Button button in choiceButtons)
        {
            button.gameObject.SetActive(false);
            Destroy(button.gameObject);
        }
        choiceButtons.Clear();

        currentNode = node;
        int officeMood = node.useEduardAtmosphere
            ? Mathf.Clamp(MistakeCountAt(historyIndex), 0, EduardMistakeLimit)
            : 0;
        visuals.SetBackground(node.useEduardAtmosphere ? EduardBackgrounds[officeMood] : node.background);
        UpdateMusic(node.useEduardAtmosphere, officeMood);
        if (IsRetryNode)
        {
            visuals.SetCharacterSprite(null);
            lineLength = 0;
            visibleCharacters = 0f;
            backButton.interactable = false;
            forwardButton.interactable = false;
            forwardButton.gameObject.SetActive(false);
            gameOverSequence = StartCoroutine(PlayGameOverSequence());
            return;
        }

        if (node.id == "ending_death_2" || node.id == "ending_secret_3")
        {
            lineLength = 0;
            visibleCharacters = 0f;
            if (node.id == "ending_death_2")
                PlayDeathEnding();
            else
                PlaySecretEnding();
            return;
        }

        if (node.id == "finale_3")
        {
            dialoguePanel.SetActive(false);
            visuals.SetCharacterSprite(null);
            lineLength = 0;
            visibleCharacters = 0f;
            UpdateHistoryButtons();
            goToElevatorButton.gameObject.SetActive(true);
            goToElevatorButton.interactable = true;
            wetSignButton.gameObject.SetActive(true);
            wetSignButton.interactable = true;
            finaleSequence = StartCoroutine(PulseElevatorButton());
            return;
        }

        if (node.id == "ending_death_1")
        {
            dialoguePanel.SetActive(false);
            visuals.SetCharacterSprite(node.characterSprite);
            lineLength = 0;
            visibleCharacters = 0f;
            finaleSequence = StartCoroutine(PlaySlowFallAnimation());
            return;
        }

        dialoguePanel.SetActive(true);
        visuals.SetCharacterSprite(node.useEduardAtmosphere ? EduardPortraits[officeMood] : node.characterSprite);
        speakerText.text = node.characterName ?? string.Empty;

        dialogueText.text = node.text ?? string.Empty;
        dialogueText.maxVisibleCharacters = showImmediately ? int.MaxValue : 0;
        dialogueText.ForceMeshUpdate();
        lineLength = dialogueText.textInfo.characterCount;
        visibleCharacters = showImmediately ? lineLength : 0f;
        UpdateHistoryButtons();

        if (visibleCharacters >= lineLength)
            ShowActions();
    }

    private void UpdateHistoryButtons()
    {
        bool canNavigate = !isFalling && !isEnding && currentNode?.id != "finale_3";
        backButton.interactable = historyIndex > 0 && canNavigate;
        bool canGoForward = CanGoForward && canNavigate;
        forwardButton.interactable = canGoForward;
        forwardButton.gameObject.SetActive(canGoForward);
    }

    private IEnumerator PulseElevatorButton()
    {
        float startedAt = Time.unscaledTime;
        while (true)
        {
            float phase = (Time.unscaledTime - startedAt) * (2f * Mathf.PI / ElevatorPulsePeriod);
            float scale = 1f + 0.025f * (1f - Mathf.Cos(phase));
            goToElevatorButton.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }

    private IEnumerator PlaySlowFallAnimation()
    {
        isFalling = true;
        UpdateHistoryButtons();
        Rect screen = ((RectTransform)characterRect.parent).rect;
        Vector2 fallenPosition = characterRestPosition + new Vector2(screen.width * 0.22f, -screen.height * 1.1f);
        float startedAt = Time.unscaledTime;
        for (float progress = 0f; progress < 1f; progress = (Time.unscaledTime - startedAt) / FallDurationSeconds)
        {
            characterRect.anchoredPosition = Vector2.Lerp(characterRestPosition, fallenPosition, progress * progress);
            yield return null;
        }

        characterRect.anchoredPosition = fallenPosition;
        cameraShake.Shake(ImpactShakeSeconds, 0.30f);
        speakerText.text = currentNode.characterName ?? string.Empty;
        dialogueText.text = currentNode.text ?? string.Empty;
        dialogueText.maxVisibleCharacters = int.MaxValue;
        dialoguePanel.SetActive(true);
        backButton.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(FallCaptionSeconds);

        isFalling = false;
        finaleSequence = null;
        ShowNode(currentNode.nextNodeId);
    }

    private void ChooseSecretEnding()
    {
        if (isActiveAndEnabled && currentNode?.id == "finale_3" &&
            wetSignButton.isActiveAndEnabled && wetSignButton.interactable)
            ShowNode("ending_secret_1");
    }

    private void GoToElevator()
    {
        if (isActiveAndEnabled && currentNode?.id == "finale_3" &&
            goToElevatorButton.isActiveAndEnabled && goToElevatorButton.interactable)
            ShowNode("ending_death_1");
    }

    private void ReturnToFinale()
    {
        if (isActiveAndEnabled && isEnding && creditsBackButton.isActiveAndEnabled && creditsBackButton.interactable)
            RestartFromNode("finale_3");
    }

    private void PlayDeathEnding() =>
        finaleSequence = StartCoroutine(PlayEndingSequence(DeathEndingText, DeathEndingText));

    private void PlaySecretEnding() =>
        finaleSequence = StartCoroutine(PlayEndingSequence(currentNode.text,
            "Вы были внимательны. В этот раз."));

    private IEnumerator PlayEndingSequence(string finalText, string creditsEpilogue)
    {
        isEnding = true;
        dialoguePanel.SetActive(false);
        SetContinuationVisible(false);
        UpdateHistoryButtons();
        endingText.text = finalText ?? string.Empty;
        endingText.maxVisibleCharacters = 0;
        endingTextFade.alpha = 1f;
        endingText.gameObject.SetActive(true);
        creditsText.gameObject.SetActive(false);
        creditsBackButton.interactable = true;
        creditsPanel.SetActive(true);
        endingText.ForceMeshUpdate();

        int length = endingText.textInfo.characterCount;
        float typed = 0f;
        while (typed < length)
        {
            typed = Mathf.Min(length, typed + Time.unscaledDeltaTime / EndingCharacterSeconds);
            endingText.maxVisibleCharacters = (int)typed;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(EndingReadPauseSeconds);
        float fadeStartedAt = Time.unscaledTime;
        for (float elapsed = 0f; elapsed < EndingTextFadeSeconds; elapsed = Time.unscaledTime - fadeStartedAt)
        {
            endingTextFade.alpha = 1f - elapsed / EndingTextFadeSeconds;
            yield return null;
        }

        endingTextFade.alpha = 0f;
        endingText.gameObject.SetActive(false);
        creditsText.text = $"{creditsContent}\n\n---\n\n{creditsEpilogue}\n\n---\n\nСпасибо за игру!";
        creditsText.gameObject.SetActive(true);
        RectTransform creditsRect = creditsText.rectTransform;
        float panelHeight = ((RectTransform)creditsPanel.transform).rect.height;
        float textHeight = Mathf.Max(1f, creditsText.preferredHeight);
        creditsRect.sizeDelta = new Vector2(creditsRect.sizeDelta.x, textHeight);
        creditsRect.anchoredPosition = new Vector2(0f, -40f);
        float distance = panelHeight + textHeight + 80f;
        for (float travelled = 0f; travelled < distance; travelled += Time.unscaledDeltaTime * CreditsScrollSpeed)
        {
            creditsRect.anchoredPosition = new Vector2(0f, travelled - 40f);
            yield return null;
        }

        creditsText.gameObject.SetActive(false);
        finaleSequence = null;
    }

    private void ResetFinalePresentation()
    {
        if (finaleSequence != null)
        {
            StopCoroutine(finaleSequence);
            finaleSequence = null;
        }

        if (isFalling)
            cameraShake.StopShake();
        isFalling = false;
        isEnding = false;
        backButton.gameObject.SetActive(true);
        creditsBackButton.interactable = false;
        characterRect.anchoredPosition = characterRestPosition;
        wetSignButton.interactable = false;
        wetSignButton.gameObject.SetActive(false);
        goToElevatorButton.interactable = false;
        goToElevatorButton.gameObject.SetActive(false);
        goToElevatorButton.transform.localScale = Vector3.one;
        creditsPanel.SetActive(false);
    }

    private IEnumerator PlayGameOverSequence()
    {
        dialoguePanel.SetActive(false);
        if (!currentNode.useEduardAtmosphere)
        {
            batonImage.sprite = VisualManager.LoadCharacterSprite(currentNode.characterSprite);
            if (batonImage.sprite != null)
            {
                RectTransform batonRect = batonImage.rectTransform;
                var leftCenter = new Vector2(0f, 0.5f);
                batonRect.anchorMin = batonRect.anchorMax = batonRect.pivot = leftCenter;
                batonRect.sizeDelta = new Vector2(2000f, 2000f);
                batonRect.anchoredPosition = new Vector2(-1500f, 0f);
                batonImage.color = new Color(1f, 1f, 1f, 0f);
                batonImage.gameObject.SetActive(true);

                float swingStartedAt = Time.unscaledTime;
                for (float progress = 0f; progress < 1f; progress = (Time.unscaledTime - swingStartedAt) / BatonTravelSeconds)
                {
                    batonRect.anchoredPosition = new Vector2(Mathf.SmoothStep(-1500f, 0f, progress), 0f);
                    batonImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(progress * 6f));
                    yield return null;
                }

                batonRect.anchoredPosition = Vector2.zero;
            }

            cameraShake.Shake(ImpactShakeSeconds, 0.30f);
            batonImage.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(PostImpactPauseSeconds);
        }

        blackoutFade.alpha = 1f;
        gameOverTitleFade.alpha = 0f;
        retryFade.alpha = 0f;
        retryFade.blocksRaycasts = false;
        retryButton.interactable = false;
        gameOverPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(BlackScreenPauseSeconds);

        float titleStartedAt = Time.unscaledTime;
        for (float elapsed = 0f; elapsed < GameOverTitleFadeSeconds; elapsed = Time.unscaledTime - titleStartedAt)
        {
            gameOverTitleFade.alpha = elapsed / GameOverTitleFadeSeconds;
            yield return null;
        }

        gameOverTitleFade.alpha = 1f;
        yield return new WaitForSecondsRealtime(GameOverRetryDelaySeconds);

        float retryStartedAt = Time.unscaledTime;
        for (float elapsed = 0f; elapsed < GameOverRetryFadeSeconds; elapsed = Time.unscaledTime - retryStartedAt)
        {
            retryFade.alpha = elapsed / GameOverRetryFadeSeconds;
            yield return null;
        }

        retryFade.alpha = 1f;
        retryButton.interactable = HasTarget(currentNode.retryNodeId);
        retryFade.blocksRaycasts = retryButton.interactable;
        gameOverSequence = null;
    }

    private void ResetGameOverPresentation()
    {
        if (gameOverSequence != null)
        {
            StopCoroutine(gameOverSequence);
            gameOverSequence = null;
        }

        cameraShake.StopShake();
        batonImage.gameObject.SetActive(false);
        batonImage.sprite = null;
        gameOverPanel.SetActive(false);
        retryButton.interactable = false;
        retryFade.blocksRaycasts = false;
    }

    private void Update()
    {
        if (isEnding)
            return;

        if (visibleCharacters < lineLength)
        {
            float speed = Mathf.Max(1f, charactersPerSecond);
            visibleCharacters = Mathf.Min(lineLength, visibleCharacters + Time.unscaledDeltaTime * speed);
            dialogueText.maxVisibleCharacters = (int)visibleCharacters;

            if (visibleCharacters >= lineLength)
                ShowActions();
            return;
        }

        if (autoAdvanceAt.HasValue && Time.unscaledTime >= autoAdvanceAt.Value)
        {
            autoAdvanceAt = null;
            AdvanceToNextNode();
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
        if (isFalling || isEnding || currentNode.id == "finale_3")
            return;

        if (currentNode.choices == null || currentNode.choices.Length == 0)
        {
            bool hasNext = CanGoForward || HasTarget(currentNode.nextNodeId);
            if (hasNext && currentNode.autoAdvanceDelaySeconds > 0f)
            {
                autoAdvanceAt = Time.unscaledTime + currentNode.autoAdvanceDelaySeconds;
                return;
            }

            SetContinuationVisible(hasNext);
            return;
        }

        foreach (Choice choice in currentNode.choices)
        {
            if (choice == null)
                continue;

            Button button = Instantiate(choiceButtonTemplate, choicesContainer, false);
            button.GetComponentInChildren<TMP_Text>(true).text = choice.choiceText ?? string.Empty;
            button.interactable = HasTarget(choice.nextNodeId) &&
                (string.IsNullOrWhiteSpace(choice.mismatchNodeId) || HasTarget(choice.mismatchNodeId));
            if (button.interactable)
                button.onClick.AddListener(() => SelectChoice(choice));

            choiceButtons.Add(button);
            button.gameObject.SetActive(true);
        }
    }

    private void SelectChoice(Choice choice)
    {
        if (string.IsNullOrWhiteSpace(choice.mismatchNodeId))
        {
            bool addsMistake = currentNode.useEduardAtmosphere && CountsAsMistake(choice);
            VisitNode(choice.nextNodeId, choice.flagToSet, addsMistake);
            return;
        }

        bool matchesFlag = HasFlagAt(choice.flagToSet, historyIndex);
        string target = matchesFlag ? choice.nextNodeId : choice.mismatchNodeId;
        VisitNode(target, null, currentNode.useEduardAtmosphere && !matchesFlag);
    }

    private bool CountsAsMistake(Choice choice)
    {
        if (choice.addsMistake || (!string.IsNullOrWhiteSpace(choice.mistakeUnlessFlag) &&
            !HasFlagAt(choice.mistakeUnlessFlag, historyIndex)))
            return true;

        foreach (string flag in choice.mistakeIfAnyFlags ?? Array.Empty<string>())
        {
            if (HasFlagAt(flag, historyIndex))
                return true;
        }

        return false;
    }

    private void UpdateMusic(bool inOffice, int officeMood)
    {
        AudioSource nextMusic = null;
        if (inOffice)
        {
            nextMusic = officeMood switch
            {
                0 => musicNeutral,
                EduardMistakeLimit => musicHorror,
                _ => musicTense,
            };
        }

        if (currentMusic == nextMusic)
            return;

        if (currentMusic != null)
            currentMusic.Stop();

        currentMusic = nextMusic;
        if (currentMusic != null && currentMusic.clip != null && currentMusic.isActiveAndEnabled)
            currentMusic.Play();
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
        if (!isActiveAndEnabled || isFalling || isEnding || currentNode?.id == "finale_3" ||
            !advanceArea.isActiveAndEnabled || !advanceArea.interactable)
            return;

        AdvanceToNextNode();
    }

    private void AdvanceToNextNode()
    {
        if (currentNode.id == "finale_3")
            return;

        if (CanGoForward)
            GoForward();
        else
            ShowNode(currentNode.nextNodeId);
    }

    private void GoBack() => MoveInHistory(-1);

    private void GoForward() => MoveInHistory(1);

    private void Retry()
    {
        if (isActiveAndEnabled && retryButton.isActiveAndEnabled && retryButton.interactable)
            RestartFromNode(currentNode.retryNodeId);
    }

    private void MoveInHistory(int offset)
    {
        int target = historyIndex + offset;
        if (!isActiveAndEnabled || IsRetryNode || isFalling || isEnding || currentNode?.id == "finale_3" ||
            target < 0 || target >= history.Count)
            return;

        historyIndex = target;
        DisplayNode(history[historyIndex].Node, showImmediately: true);
    }

    private void OnDestroy()
    {
        if (currentMusic != null)
            currentMusic.Stop();

        if (advanceArea != null)
            advanceArea.onClick.RemoveListener(Advance);
        if (backButton != null)
            backButton.onClick.RemoveListener(GoBack);
        if (forwardButton != null)
            forwardButton.onClick.RemoveListener(GoForward);
        if (retryButton != null)
            retryButton.onClick.RemoveListener(Retry);
        if (wetSignButton != null)
            wetSignButton.onClick.RemoveListener(ChooseSecretEnding);
        if (goToElevatorButton != null)
            goToElevatorButton.onClick.RemoveListener(GoToElevator);
        if (creditsBackButton != null)
            creditsBackButton.onClick.RemoveListener(ReturnToFinale);
    }
}
