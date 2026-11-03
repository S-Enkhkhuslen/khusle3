using System;
using System.Collections.Generic;
using UnityEngine;

// Local scores survive scene restarts and application restarts.
public static class ScoreLeaderboard
{
    public const int Capacity = 10;
    private const string SaveKey = "Khusle.TopScores.v1";

    [Serializable]
    public struct Entry
    {
        public int score;
        public bool escaped;
    }

    [Serializable]
    private class SaveData { public List<Entry> entries = new List<Entry>(); }
    private static List<Entry> entries;
    public static IReadOnlyList<Entry> Entries { get { Load(); return entries; } }

    // Equal scores keep their earlier position; a new tie is placed after them.
    private static int Insert(Entry entry)
    {
        int index = entries.FindIndex(previous => previous.score < entry.score);
        if (index < 0) index = entries.Count;
        entries.Insert(index, entry);
        if (entries.Count > Capacity) entries.RemoveAt(Capacity);
        return index < Capacity ? index + 1 : 0;
    }

    public static int Add(int score, bool escaped)
    {
        Load();
        int rank = Insert(new Entry { score = Mathf.Max(0, score), escaped = escaped });
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new SaveData { entries = entries }));
        PlayerPrefs.Save();
        return rank;
    }

    private static void Load()
    {
        if (entries != null) return;
        entries = new List<Entry>();
        if (!PlayerPrefs.HasKey(SaveKey)) return;
        try
        {
            var saved = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            if (saved == null || saved.entries == null) return;
            foreach (Entry entry in saved.entries)
                if (entry.score >= 0) Insert(entry);
        }
        catch (ArgumentException)
        {
            // An unreadable old save should not prevent a new game from starting.
            Debug.LogWarning("ScoreLeaderboard: Could not read saved scores.");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() { entries = null; }
}
