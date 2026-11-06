using UCTWebGame.Engine;
using UCTWebGame.Models;

namespace UCTWebGame.Services;

/// <summary>
/// Every NPC uses the same strategy and explains each decision:
///  - Reinforce: lift weak countries to 3 soldiers (spending at most half), stack the rest on the strongest country.
///  - Attack: only when the best attacker has a clear advantage (2+ more attacking soldiers than the target's defenders),
///    choosing the biggest advantage, then the biggest conquest bonus.
///  - Stop: retreat once the attackers no longer outnumber the defenders.
///  - Transfer: after attacking, move spare soldiers to a country that has only 1-2 soldiers.
/// </summary>
public sealed class NpcService : INpcService
{
    private const int MinimumAdvantageToAttack = 2;
    private const int WeakCountryThreshold = 3;

    private readonly GameEngine _engine;

    public NpcService(GameEngine engine)
    {
        _engine = engine;
    }

    public List<GameLogEntry> PlayUntilHumanTurn(GameEvent game)
    {
        var log = new List<GameLogEntry>();
        var wasInProgress = game.Status == GameStatus.InProgress;

        // A normal pass visits each NPC once before returning to the human; the bound also
        // stops corrupted turn data from keeping a request alive.
        var maximumNpcTurns = Math.Max(1, game.TurnOrder.Count);
        for (var turn = 0; turn < maximumNpcTurns && game.Status == GameStatus.InProgress; turn++)
        {
            var current = _engine.GetCurrentPlayer(game);
            if (current.Type == PlayerType.Human)
                break;

            try
            {
                PlayTurn(game, current, log);
            }
            catch (InvalidOperationException)
            {
                // Never let one NPC bug lock the human out of the game.
                RecoverTurn(game, current);
            }
        }

        if (wasInProgress && game.Status == GameStatus.Finished)
            log.Add(GameOverEntry(game));

        return log;
    }

    // ---------- One NPC turn ----------

    private void PlayTurn(GameEvent game, Player player, List<GameLogEntry> log)
    {
        Reinforce(game, player, log);
        if (!IsMyTurn(game, player)) return;

        string? skipReason = null;

        if (game.Phase == TurnPhase.Attacking && game.PendingAttack is not null)
        {
            // Recovery path: an attack was left half-finished.
            var attacker = Country(game, game.PendingAttack.AttackerCode);
            var defender = Country(game, game.PendingAttack.DefenderCode);
            Fight(game, player, attacker, defender, log);
        }
        else if (game.Phase == TurnPhase.Action && !game.AttackUsedThisTurn)
        {
            var plan = ChooseAttack(game, player, out skipReason);
            if (plan is not null)
            {
                _engine.StartAttack(game, player.Id, plan.Attacker.Code, plan.Defender.Code);
                log.Add(Entry(
                    game, player, LogTypes.AttackStart,
                    $"{player.Name} attacks {plan.Defender.Name} from {plan.Attacker.Name}.",
                    $"{plan.Attacker.Name} has {plan.Attacker.Soldiers} soldiers against {plan.Defender.Name}'s {plan.Defender.Soldiers}, " +
                    $"a clear advantage of {plan.Advantage}. Conquering it would add {plan.Bonus} soldiers.",
                    Array.Empty<string>(),
                    attackerCode: plan.Attacker.Code,
                    defenderCode: plan.Defender.Code));

                Fight(game, player, plan.Attacker, plan.Defender, log);
                skipReason = null;
            }
        }

        // A retreat passes the turn and a conquest of the whole world ends the game.
        if (!IsMyTurn(game, player)) return;

        Fortify(game, player, log);

        if (IsMyTurn(game, player) && game.Phase == TurnPhase.Action)
        {
            log.Add(Entry(
                game, player, LogTypes.EndTurn,
                $"{player.Name} ends the turn.",
                skipReason ?? "The attack and any troop moves for this turn are complete.",
                Array.Empty<string>()));
            _engine.Skip(game, player.Id);
        }
    }

    // ---------- Reinforcements ----------

    private void Reinforce(GameEvent game, Player player, List<GameLogEntry> log)
    {
        if (game.Phase != TurnPhase.Reinforce || game.PendingReinforcements <= 0) return;

        var owned = game.Countries.Where(c => c.OwnerId == player.Id).ToList();
        if (owned.Count == 0) return;

        var total = game.PendingReinforcements;
        var remaining = total;
        var budget = total / 2;                       // at most half goes to weak countries
        var allocation = new Dictionary<string, int>();
        var after = owned.ToDictionary(c => c.Code, c => c.Soldiers);
        var lifted = new List<string>();

        foreach (var country in owned.Where(c => c.Soldiers < WeakCountryThreshold).OrderBy(c => c.Soldiers).ThenBy(c => c.Code))
        {
            var need = Math.Min(WeakCountryThreshold - country.Soldiers, budget);
            if (need <= 0) continue;

            allocation[country.Code] = allocation.GetValueOrDefault(country.Code) + need;
            after[country.Code] += need;
            budget -= need;
            remaining -= need;
            lifted.Add(country.Name);
        }

        var best = owned.OrderByDescending(c => after[c.Code]).ThenBy(c => c.Code).First();
        allocation[best.Code] = allocation.GetValueOrDefault(best.Code) + remaining;

        allocation = allocation.Where(pair => pair.Value > 0).ToDictionary(pair => pair.Key, pair => pair.Value);
        _engine.ApplyReinforcements(game, player.Id, allocation);

        var summary = string.Join(", ", allocation.Select(pair => $"{pair.Value} on {Country(game, pair.Key).Name}"));
        var why = lifted.Count > 0
            ? $"{best.Name} is the strongest base to attack from. {string.Join(" and ", lifted)} " +
              $"{(lifted.Count == 1 ? "was" : "were")} lifted toward {WeakCountryThreshold} soldiers so they are not easy targets."
            : $"{best.Name} is the strongest country, so stacking it builds the best attacking force.";

        log.Add(Entry(
            game, player, LogTypes.Reinforce,
            $"{player.Name} places {total} reinforcements: {summary}.",
            why,
            allocation.Keys.ToArray()));
    }

    // ---------- Choosing and fighting an attack ----------

    private sealed record AttackPlan(CountryState Attacker, CountryState Defender, int Advantage, int Bonus);

    private static AttackPlan? ChooseAttack(GameEvent game, Player player, out string? skipReason)
    {
        skipReason = null;

        var mine = game.Countries.Where(c => c.OwnerId == player.Id && c.Soldiers >= 2).ToList();
        var enemies = game.Countries.Where(c => c.OwnerId != player.Id).ToList();

        if (enemies.Count == 0) return null;
        if (mine.Count == 0)
        {
            skipReason = "No country has 2 or more soldiers, so nobody can attack.";
            return null;
        }

        AttackPlan? best = null;
        foreach (var attacker in mine)
        {
            foreach (var defender in enemies)
            {
                var plan = new AttackPlan(
                    attacker,
                    defender,
                    Advantage: (attacker.Soldiers - 1) - defender.Soldiers,
                    Bonus: Math.Max(2, defender.StartingSoldiers / 2));

                if (best is null || IsBetter(plan, best))
                    best = plan;
            }
        }

        if (best!.Advantage < MinimumAdvantageToAttack)
        {
            skipReason =
                $"No clear advantage: the best option was {best.Attacker.Name} ({best.Attacker.Soldiers}) against " +
                $"{best.Defender.Name} ({best.Defender.Soldiers}). Waiting for reinforcements.";
            return null;
        }

        return best;
    }

    private static bool IsBetter(AttackPlan candidate, AttackPlan current) =>
        candidate.Advantage != current.Advantage ? candidate.Advantage > current.Advantage :
        candidate.Bonus != current.Bonus ? candidate.Bonus > current.Bonus :
        string.CompareOrdinal(candidate.Defender.Code, current.Defender.Code) < 0;

    private void Fight(GameEvent game, Player player, CountryState attacker, CountryState defender, List<GameLogEntry> log)
    {
        for (var round = 0; round < 100 && game.Status == GameStatus.InProgress && game.Phase == TurnPhase.Attacking; round++)
        {
            // From the second round on, keep going only while the attackers still outnumber the defenders.
            if (round > 0 && attacker.Soldiers - 1 <= defender.Soldiers)
            {
                Retreat(game, player, attacker, defender, log,
                    $"Odds turned: only {attacker.Soldiers - 1} attacking soldier(s) left against {defender.Soldiers} defenders.");
                return;
            }

            var result = _engine.ResolveDiceRound(game);

            log.Add(Entry(
                game, player, LogTypes.DiceRound,
                $"{attacker.Name} vs {defender.Name}: attackers lose {result.AttackerLosses}, defenders lose {result.DefenderLosses}.",
                null,
                new[] { attacker.Code, defender.Code },
                result, attacker.Code, defender.Code));

            if (result.Conquered)
            {
                log.Add(Entry(
                    game, player, LogTypes.Conquest,
                    $"{player.Name} conquers {defender.Name} and garrisons it with {result.SoldiersGained} soldiers.",
                    null,
                    new[] { attacker.Code, defender.Code }));

                if (result.EliminatedPlayerId is not null)
                {
                    var eliminated = game.Players.First(p => p.Id == result.EliminatedPlayerId);
                    log.Add(Entry(
                        game, player, LogTypes.Eliminated,
                        $"{eliminated.Name} has been eliminated.",
                        null,
                        Array.Empty<string>()));
                }
                return;
            }

            if (result.AttackEnded)
            {
                log.Add(Entry(
                    game, player, LogTypes.AttackFailed,
                    $"{player.Name}'s attack on {defender.Name} fails; {attacker.Name} is down to 1 soldier.",
                    "A run of bad rolls left too few soldiers to continue.",
                    new[] { attacker.Code, defender.Code }));
                return;
            }
        }

        // Safety net: the round cap was hit without a result.
        if (game.Status == GameStatus.InProgress && game.Phase == TurnPhase.Attacking)
            Retreat(game, player, attacker, defender, log, "The battle dragged on too long, so the attack is called off.");
    }

    private void Retreat(GameEvent game, Player player, CountryState attacker, CountryState defender, List<GameLogEntry> log, string why)
    {
        // Log first: stopping an attack ends the turn and moves the game on.
        log.Add(Entry(
            game, player, LogTypes.Retreat,
            $"{player.Name} retreats from {defender.Name}.",
            why,
            new[] { attacker.Code, defender.Code }));

        _engine.StopAttack(game, player.Id);
    }

    // ---------- Moving soldiers ----------

    private void Fortify(GameEvent game, Player player, List<GameLogEntry> log)
    {
        var owned = game.Countries.Where(c => c.OwnerId == player.Id).ToList();
        if (owned.Count < 2) return;

        var weakest = owned.OrderBy(c => c.Soldiers).ThenBy(c => c.Code).First();
        var strongest = owned.OrderByDescending(c => c.Soldiers).ThenBy(c => c.Code).First();

        if (weakest.Code == strongest.Code || weakest.Soldiers > 2 || strongest.Soldiers < 5) return;

        var before = weakest.Soldiers;
        var amount = Math.Max(1, Math.Min((strongest.Soldiers - weakest.Soldiers) / 3, strongest.Soldiers - 1));

        _engine.Transfer(game, player.Id, strongest.Code, weakest.Code, amount);

        log.Add(Entry(
            game, player, LogTypes.Transfer,
            $"{player.Name} moves {amount} soldier(s) from {strongest.Name} to {weakest.Name}.",
            $"{weakest.Name} had only {before} soldier(s) and would be the easiest target, while {strongest.Name} had soldiers to spare.",
            new[] { strongest.Code, weakest.Code }));
    }

    // ---------- Helpers ----------

    private bool IsMyTurn(GameEvent game, Player player) =>
        game.Status == GameStatus.InProgress && _engine.GetCurrentPlayer(game).Id == player.Id;

    private static CountryState Country(GameEvent game, string code) =>
        game.Countries.First(c => c.Code == code);

    private static GameLogEntry Entry(
        GameEvent game,
        Player player,
        string type,
        string message,
        string? justification,
        string[] changedCodes,
        RoundResult? result = null,
        string? attackerCode = null,
        string? defenderCode = null) => new()
        {
            Round = game.Round,
            PlayerId = player.Id,
            PlayerName = player.Name,
            Type = type,
            Message = message,
            Justification = justification,
            Result = result,
            AttackerCode = attackerCode,
            DefenderCode = defenderCode,
            Changes = GameLog.Snapshot(game, changedCodes.Distinct().ToArray())
        };

    private static GameLogEntry GameOverEntry(GameEvent game)
    {
        var winner = game.Players.FirstOrDefault(p => p.Id == game.WinnerId);
        return new GameLogEntry
        {
            Round = game.Round,
            PlayerId = winner?.Id ?? string.Empty,
            PlayerName = winner?.Name ?? "Campaign",
            Type = LogTypes.GameOver,
            Message = winner is not null
                ? $"{winner.Name} now controls the world. The campaign is over."
                : "Your last country has fallen. The campaign is over."
        };
    }

    // If an NPC turn throws, finish it in the simplest legal way so the human is never stuck.
    private void RecoverTurn(GameEvent game, Player player)
    {
        try
        {
            if (!IsMyTurn(game, player)) return;

            if (game.Phase == TurnPhase.Reinforce && game.PendingReinforcements > 0)
            {
                var first = game.Countries.First(c => c.OwnerId == player.Id);
                _engine.ApplyReinforcements(game, player.Id,
                    new Dictionary<string, int> { [first.Code] = game.PendingReinforcements });
            }

            if (!IsMyTurn(game, player)) return;

            if (game.Phase == TurnPhase.Attacking) _engine.StopAttack(game, player.Id);
            else _engine.Skip(game, player.Id);
        }
        catch (InvalidOperationException)
        {
            // Nothing more can safely be done here.
        }
    }
}