using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class FlagsManager
{
    public static FlagsManager Instance { get; } = new FlagsManager();

    private readonly HashSet<string> flags = new HashSet<string>(StringComparer.Ordinal);

    private FlagsManager() { }

    public void SetFlag(string flag)
    {
        if (!string.IsNullOrWhiteSpace(flag))
            flags.Add(flag);
    }

    public bool HasFlag(string flag) => flags.Contains(flag);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlaySession() => Instance.flags.Clear();
}
