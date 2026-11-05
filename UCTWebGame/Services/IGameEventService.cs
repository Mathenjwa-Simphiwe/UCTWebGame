using UCTWebGame.Models;

namespace UCTWebGame.Services;

public interface IGameEventService
{
    Task<GameEvent> CreateAsync(GameEvent game, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameEvent>> GetForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<GameEvent?> GetByIdForUserAsync(
        string gameId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<bool> ReplaceForUserAsync(
        GameEvent game,
        int expectedVersion,
        CancellationToken cancellationToken = default);
}
