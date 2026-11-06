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
}

[Serializable]
public sealed class Choice
{
    public string choiceText;
    public string nextNodeId;
    public string flagToSet;
    public string mismatchNodeId;
}

[Serializable]
public sealed class GameDataContainer
{
    public DialogueNode[] nodes;
}
