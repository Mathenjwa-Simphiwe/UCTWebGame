using UCTWebGame.Models;

namespace UCTWebGame.Services;

public interface INpcService
{
    void PlayUntilHumanTurn(GameEvent game);
}
