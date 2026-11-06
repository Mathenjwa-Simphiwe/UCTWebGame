using UCTWebGame.Models;

namespace UCTWebGame.Services;

public interface INpcService
{
    // Plays NPC turns until it is the human's turn (or the game ends) and returns what happened,
    // in order, with a short justification for every decision.
    List<GameLogEntry> PlayUntilHumanTurn(GameEvent game);
}