using System;

[Serializable]
public sealed class DialogueNode
{
    public string id;
    public string characterName;
    public string text;
    public string background;
    public string characterSprite;
    public string nextNodeId;
    public Choice[] choices;
    public string requiredFlag;
    public string missingFlagNodeId;
    public string retryNodeId;
    public float autoAdvanceDelaySeconds;
    public bool useEduardAtmosphere;
}

[Serializable]
public sealed class Choice
{
    public string choiceText;
    public string nextNodeId;
    public string flagToSet;
    public string mismatchNodeId;
    public bool addsMistake;
    public string mistakeUnlessFlag;
    public string[] mistakeIfAnyFlags;
}

[Serializable]
public sealed class GameDataContainer
{
    public DialogueNode[] nodes;
}
