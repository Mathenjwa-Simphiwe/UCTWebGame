using UCTWebGame.Engine;
using UCTWebGame.Models;

namespace UCTWebGame.Services;

/// <summary>
/// Runs simple NPC turns synchronously until control returns to the human commander.
/// </summary>
public sealed class NpcService : INpcService
{
    private readonly GameEngine _engine;

    public NpcService(GameEngine engine)
    {
        _engine = engine;
    }

    public void PlayUntilHumanTurn(GameEvent game)
    {
        // A normal pass visits each of the nine NPCs once before returning to the human.
        // The bound also prevents corrupted turn data from keeping an API request alive.
        var maximumNpcTurns = Math.Max(1, game.TurnOrder.Count);
        for (var turn = 0; turn < maximumNpcTurns && game.Status == GameStatus.InProgress; turn++)
        {
            var current = _engine.GetCurrentPlayer(game);
            if (current.Type == PlayerType.Human)
                return;

            PlayTurn(game, current);
        }
    }

    private void PlayTurn(GameEvent game, Player player)
    {
        if (game.PendingReinforcements > 0 && game.Phase == TurnPhase.Reinforce)
        {
            var ownedCountries = game.Countries
                .Where(country => country.OwnerId == player.Id)
                .ToList();
            var mostSoldiers = ownedCountries.Count == 0 ? 0 : ownedCountries.Max(country => country.Soldiers);
            var strongestCountry = ownedCountries
                .Where(country => country.Soldiers == mostSoldiers)
                .OrderBy(_ => Random.Shared.Next())
                .FirstOrDefault();

            if (strongestCountry is not null)
            {
                _engine.ApplyReinforcements(
                    game,
                    player.Id,
                    new Dictionary<string, int>
                    {
                        [strongestCountry.Code] = game.PendingReinforcements
                    });
            }
        }

        if (game.Status != GameStatus.InProgress || _engine.GetCurrentPlayer(game).Id != player.Id)
            return;

        if (game.Phase == TurnPhase.Attacking && game.PendingAttack is not null)
        {
            ResolveAttack(game, player.Id);
        }
        else if (game.Phase == TurnPhase.Action && !game.AttackUsedThisTurn)
        {
            var attackableCountries = game.Countries
                .Where(country => country.OwnerId == player.Id && country.Soldiers >= 2)
                .ToList();
            var strongestForce = attackableCountries.Count == 0 ? 0 : attackableCountries.Max(country => country.Soldiers);
            var attacker = attackableCountries
                .Where(country => country.Soldiers == strongestForce)
                .OrderBy(_ => Random.Shared.Next())
                .FirstOrDefault();
            var defender = attacker is null
                ? null
                : game.Countries
                    .Where(country => country.OwnerId != player.Id)
                    .OrderBy(country => country.Soldiers)
                    .ThenBy(_ => Random.Shared.Next())
                    .FirstOrDefault();

            if (attacker is not null && defender is not null)
            {
                _engine.StartAttack(game, player.Id, attacker.Code, defender.Code);
                ResolveAttack(game, player.Id);
            }
        }

        if (game.Status == GameStatus.InProgress &&
            _engine.GetCurrentPlayer(game).Id == player.Id &&
            game.Phase == TurnPhase.Action)
        {
            _engine.Skip(game, player.Id);
        }
    }

    private void ResolveAttack(GameEvent game, string playerId)
    {
        var maximumDiceRounds = 100;
        while (maximumDiceRounds-- > 0 &&
               game.Status == GameStatus.InProgress &&
               game.Phase == TurnPhase.Attacking)
        {
            var result = _engine.ResolveDiceRound(game);
            if (result.AttackEnded || result.GameOver)
                break;
        }

        if (game.Status == GameStatus.InProgress && game.Phase == TurnPhase.Attacking)
            _engine.StopAttack(game, playerId);
    }
}
