#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Unity's Shift+Space shortcut is processed before gameplay sees the key.
// Change only the in-memory Game-view shortcut flag while this project plays.
// Do not write EditorPrefs or modify the user's shortcut profile.
[InitializeOnLoad]
internal static class PlayModeKeyboardShortcuts
{
    private const string AppliedKey = "Khusle3.GameViewShortcuts.Applied";
    private const string PreviousKey = "Khusle3.GameViewShortcuts.Previous";

    private static FieldInfo IgnoreShortcutFlag => typeof(EditorApplication).Assembly
        .GetType("UnityEditor.ShortcutManagement.ShortcutIntegration")
        ?.GetField("s_IgnoreWhenPlayModeFocused", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

    static PlayModeKeyboardShortcuts()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.quitting += Restore;
        AssemblyReloadEvents.beforeAssemblyReload += Restore;
        EditorApplication.delayCall += Synchronize;
    }

    private static void Synchronize()
    {
        if (EditorApplication.isPlaying)
            Apply();
        else
            Restore();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            Apply();
        else if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            Restore();
    }

    private static void Apply()
    {
        FieldInfo flag = IgnoreShortcutFlag;
        if (flag == null || flag.FieldType != typeof(bool))
        {
            Debug.LogWarning("Could not disable Game-view Editor shortcuts. Turn off Unity Shortcuts in the Game view to use Shift+Space for gameplay.");
            return;
        }

        if (!SessionState.GetBool(AppliedKey, false))
        {
            bool previous = (bool)flag.GetValue(null);
            if (previous) return; // Respect an already-disabled shortcut preference.
            SessionState.SetBool(PreviousKey, previous);
            SessionState.SetBool(AppliedKey, true);
        }
        flag.SetValue(null, true);
    }

    private static void Restore()
    {
        if (!SessionState.GetBool(AppliedKey, false)) return;
        FieldInfo flag = IgnoreShortcutFlag;
        if (flag != null && flag.FieldType == typeof(bool) && (bool)flag.GetValue(null))
            flag.SetValue(null, SessionState.GetBool(PreviousKey, false));
        SessionState.EraseBool(AppliedKey);
        SessionState.EraseBool(PreviousKey);
    }
}
#endif
