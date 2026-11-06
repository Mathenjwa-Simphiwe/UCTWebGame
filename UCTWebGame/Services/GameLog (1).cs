using UCTWebGame.Models;

namespace UCTWebGame.Services;

public static class GameLog
{
    public const int MaxEntries = 400;

    // Current owner/soldiers of the given countries (or of every country if none are named).
    public static List<CountryChange> Snapshot(GameEvent game, params string[] codes)
    {
        IEnumerable<CountryState> source = codes.Length == 0
            ? game.Countries
            : game.Countries.Where(c => codes.Contains(c.Code));

        return source
            .Select(c => new CountryChange { Code = c.Code, Soldiers = c.Soldiers, OwnerId = c.OwnerId })
            .ToList();
    }

    // Numbers the entries, adds them to the saved log, and keeps only the latest MaxEntries.
    public static void Append(GameEvent game, IEnumerable<GameLogEntry> entries)
    {
        var list = entries.ToList();
        foreach (var entry in list)
            entry.Seq = ++game.LastSequence;

        game.Log.AddRange(list);
        if (game.Log.Count > MaxEntries)
            game.Log.RemoveRange(0, game.Log.Count - MaxEntries);
    }
}
