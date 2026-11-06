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
}

[Serializable]
public sealed class Choice
{
    public string choiceText;
    public string nextNodeId;
    public string flagToSet;
}

[Serializable]
public sealed class GameDataContainer
{
    public DialogueNode[] nodes;
}
