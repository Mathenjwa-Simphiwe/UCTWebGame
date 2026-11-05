using MongoDB.Bson;
using MongoDB.Driver;
using UCTWebGame.Models;

namespace UCTWebGame.Services;

public sealed class GameEventService : IGameEventService
{
    private readonly IMongoCollection<GameEvent> _games;

    public GameEventService(MongoDbService mongo)
    {
        _games = mongo.Games;
    }

    public async Task<GameEvent> CreateAsync(GameEvent game, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(game.UserId, out _))
            throw new ArgumentException("Game owner must be a valid MongoDB user ID.", nameof(game));

        game.Id ??= ObjectId.GenerateNewId().ToString();
        game.CreatedAt = DateTime.UtcNow;
        game.UpdatedAt = game.CreatedAt;
        game.Version = 1;

        await _games.InsertOneAsync(game, cancellationToken: cancellationToken);
        return game;
    }

    public async Task<IReadOnlyList<GameEvent>> GetForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(userId, out _))
            return Array.Empty<GameEvent>();

        return await _games
            .Find(game => game.UserId == userId)
            .SortByDescending(game => game.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<GameEvent?> GetByIdForUserAsync(
        string gameId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(gameId, out _) || !ObjectId.TryParse(userId, out _))
            return null;

        return await _games
            .Find(game => game.Id == gameId && game.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ReplaceForUserAsync(
        GameEvent game,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(game.Id) ||
            !ObjectId.TryParse(game.Id, out _) ||
            !ObjectId.TryParse(game.UserId, out _))
        {
            return false;
        }

        var previousVersion = game.Version;
        var previousUpdatedAt = game.UpdatedAt;
        game.Version = expectedVersion + 1;
        game.UpdatedAt = DateTime.UtcNow;

        var filter = Builders<GameEvent>.Filter.Eq(item => item.Id, game.Id) &
                     Builders<GameEvent>.Filter.Eq(item => item.UserId, game.UserId) &
                     Builders<GameEvent>.Filter.Eq(item => item.Version, expectedVersion);

        var result = await _games.ReplaceOneAsync(
            filter,
            game,
            cancellationToken: cancellationToken);

        if (result.ModifiedCount == 1)
            return true;

        game.Version = previousVersion;
        game.UpdatedAt = previousUpdatedAt;
        return false;
    }
}
