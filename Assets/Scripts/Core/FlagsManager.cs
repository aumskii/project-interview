using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class FlagsManager
{
    public static FlagsManager Instance { get; } = new FlagsManager();

    private readonly HashSet<string> flags = new HashSet<string>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> choiceFlagCounts =
        new Dictionary<string, int>(StringComparer.Ordinal);
    private int mistakeCount;

    public int MistakeCount => mistakeCount;

    private FlagsManager() { }

    public void AddMistake() => mistakeCount++;

    internal void RemoveMistake() => mistakeCount = Math.Max(0, mistakeCount - 1);

    public void SetFlag(string flag)
    {
        if (!string.IsNullOrWhiteSpace(flag))
            flags.Add(flag);
    }

    internal void AddChoiceFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
            return;

        choiceFlagCounts.TryGetValue(flag, out int count);
        choiceFlagCounts[flag] = count + 1;
    }

    internal void RemoveChoiceFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag) || !choiceFlagCounts.TryGetValue(flag, out int count))
            return;

        if (count == 1)
            choiceFlagCounts.Remove(flag);
        else
            choiceFlagCounts[flag] = count - 1;
    }

    public bool HasFlag(string flag) => HasFlagAfterRemovingChoices(flag, 0);

    internal bool HasFlagAfterRemovingChoices(string flag, int removedChoices) =>
        flag != null && (flags.Contains(flag) ||
            (choiceFlagCounts.TryGetValue(flag, out int count) && count > removedChoices));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlaySession()
    {
        Instance.flags.Clear();
        Instance.choiceFlagCounts.Clear();
        Instance.mistakeCount = 0;
    }
}
