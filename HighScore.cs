using System.Text.Json;

namespace Zelos.Minesweeper;

/// <summary>
/// Persists best completion times per difficulty key to a local JSON file.
/// </summary>
public class HighScoreManager
{
    private const string FILE_PATH = "highscores.json";
    private Dictionary<string, int> records = new();

    public HighScoreManager()
    {
        Load();
    }

    /// <summary>Returns the best time (seconds) recorded for the given difficulty key, or null.</summary>
    public int? GetBest(string key) =>
        records.TryGetValue(key, out int t) ? t : null;

    /// <summary>
    /// Compares <paramref name="seconds"/> against the stored record for <paramref name="key"/>.
    /// Updates and saves if it is a new record.
    /// </summary>
    /// <returns>True when a new record was set.</returns>
    public bool CheckAndUpdate(string key, int seconds)
    {
        if (!records.TryGetValue(key, out int best) || seconds < best)
        {
            records[key] = seconds;
            Save();
            return true;
        }
        return false;
    }

    /// <summary>Clears the best time for a specific difficulty key.</summary>
    public void ResetBest(string key)
    {
        if (records.Remove(key))
            Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(FILE_PATH))
                records = JsonSerializer.Deserialize<Dictionary<string, int>>(
                    File.ReadAllText(FILE_PATH)) ?? new();
        }
        catch
        {
            records = new();
        }
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(FILE_PATH, JsonSerializer.Serialize(records,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* silently ignore I/O errors */ }
    }
}
