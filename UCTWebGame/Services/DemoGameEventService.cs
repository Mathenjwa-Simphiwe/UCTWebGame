using System.Collections.Concurrent;
using MongoDB.Bson;
using UCTWebGame.Models;

namespace UCTWebGame.Services;

/// <summary>
/// In-memory game storage for local development without MongoDB.
/// Saved games are cleared when the API process stops.
/// </summary>
public sealed class DemoGameEventService : IGameEventService
{
    private readonly ConcurrentDictionary<string, GameEvent> _games = new();
    private readonly object _replaceLock = new();

    public Task<GameEvent> CreateAsync(GameEvent game, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ObjectId.TryParse(game.UserId, out _))
            throw new ArgumentException("Game owner must be a valid MongoDB user ID.", nameof(game));

        game.Id ??= ObjectId.GenerateNewId().ToString();
        game.CreatedAt = DateTime.UtcNow;
        game.UpdatedAt = game.CreatedAt;
        game.Version = 1;
        _games[game.Id] = Clone(game);
        return Task.FromResult(Clone(game));
    }

    public Task<IReadOnlyList<GameEvent>> GetForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<GameEvent> games = _games.Values
            .Where(game => game.UserId == userId)
            .OrderByDescending(game => game.UpdatedAt)
            .Select(Clone)
            .ToArray();
        return Task.FromResult(games);
    }

    public Task<GameEvent?> GetByIdForUserAsync(
        string gameId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_games.TryGetValue(gameId, out var game) && game.UserId == userId)
            return Task.FromResult<GameEvent?>(Clone(game));

        return Task.FromResult<GameEvent?>(null);
    }

    public Task<bool> ReplaceForUserAsync(
        GameEvent game,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_replaceLock)
        {
            if (string.IsNullOrWhiteSpace(game.Id) ||
                !_games.TryGetValue(game.Id, out var current) ||
                current.UserId != game.UserId ||
                current.Version != expectedVersion)
            {
                return Task.FromResult(false);
            }

            game.Version = expectedVersion + 1;
            game.UpdatedAt = DateTime.UtcNow;
            _games[game.Id] = Clone(game);
            return Task.FromResult(true);
        }
    }

    private static GameEvent Clone(GameEvent game) =>
        MongoDB.Bson.Serialization.BsonSerializer.Deserialize<GameEvent>(game.ToBson());
}
