using UCTWebGame.Models;

namespace UCTWebGame.Engine;

// Pure game rules: no database, SignalR, or HTTP yet.
// Every method changes the Game in place and returns something the caller can log or broadcast.
// Rule violations throw InvalidOperationException (the controller turns these into 400s).
//
// Rule decisions (reminder for me so i dont get lost in the sauce)
//  - Conquest bonus = Max(2, defender.StartingSoldiers / 2), using the country's ORIGINAL value
//    (matches the India example in the brief). The bonus soldiers become the garrison of the
//    conquered country; the player may then transfer more in from other countries.
//  - One attack per turn. After an attack ends (conquest or failure) the player may still
//    transfer soldiers, then ends the turn with Skip. Stopping an attack ends the turn immediately.
//  - New soldiers (2 per country) start from round 2 and must all be placed before acting.
//  - If the human player is eliminated the game ends (a loss).
public class GameEngine
{
    private readonly IDice _dice;
    private readonly Random _rng;

    public GameEngine(IDice dice, Random? rng = null)
    {
        _dice = dice;
        _rng = rng ?? new Random();
    }

    // ---------- Setup ----------

    public GameEvent StartGame(string ownerUserId, string chosenCountryCode, List<CountryInfo> pool)
    {
        var chosen = pool.FirstOrDefault(c => c.Code == chosenCountryCode)
            ?? throw new InvalidOperationException("Chosen country is not in the country list.");

        var others = pool.Where(c => c.Code != chosenCountryCode).ToList();
        if (others.Count < 9)
            throw new InvalidOperationException("Not enough countries in the pool to start a game.");

        Shuffle(others);
        var selected = new List<CountryInfo> { chosen };
        selected.AddRange(others.Take(9));

        var game = new GameEvent
        {
            UserId = ownerUserId,
            Status = GameStatus.InProgress,
            Round = 1,
            Phase = TurnPhase.Action,
            CurrentTurnIndex = 0
        };

        for (int i = 0; i < selected.Count; i++)
        {
            var info = selected[i];
            var playerId = $"p{i + 1}";

            game.Players.Add(new Player
            {
                Id = playerId,
                Type = i == 0 ? PlayerType.Human : PlayerType.Npc,
                Name = info.Name,
                StartCountry = info.Code,
                Eliminated = false
            });

            game.Countries.Add(new CountryState
            {
                Code = info.Code,
                Name = info.Name,
                Soldiers = info.StartingSoldiers,
                StartingSoldiers = info.StartingSoldiers,
                OwnerId = playerId
            });
        }

        game.TurnOrder = game.Players.Select(p => p.Id).ToList();
        Shuffle(game.TurnOrder);   // random first player

        return game;
    }

    // ---------- Turn flow ----------

    public Player GetCurrentPlayer(GameEvent game) => GetPlayer(game, game.TurnOrder[game.CurrentTurnIndex]);

    // Ends the current turn and starts the next non-eliminated player's turn.
    public void AdvanceTurn(GameEvent game)
    {
        game.PendingAttack = null;
        game.AttackUsedThisTurn = false;
        game.PendingReinforcements = 0;

        if (CheckGameOver(game)) return;

        int count = game.TurnOrder.Count;
        for (int step = 0; step < count; step++)
        {
            game.CurrentTurnIndex++;
            if (game.CurrentTurnIndex >= count)
            {
                game.CurrentTurnIndex = 0;
                game.Round++;
            }
            if (!GetCurrentPlayer(game).Eliminated) break;
        }

        BeginTurn(game);
    }

    private void BeginTurn(GameEvent game)
    {
        var player = GetCurrentPlayer(game);
        game.PendingReinforcements = GetReinforcementCount(game, player.Id);
        game.Phase = game.PendingReinforcements > 0 ? TurnPhase.Reinforce : TurnPhase.Action;
    }

    // 2 soldiers per country owned, from round 2 onward.
    public int GetReinforcementCount(GameEvent game, string playerId)
    {
        if (game.Round < 2) return 0;
        return 2 * OwnedBy(game, playerId).Count();
    }

    // allocation: country code -> soldiers to add. Must total exactly PendingReinforcements.
    public void ApplyReinforcements(GameEvent game, string playerId, Dictionary<string, int> allocation)
    {
        EnsureTurn(game, playerId);
        if (game.Phase != TurnPhase.Reinforce)
            throw new InvalidOperationException("There are no reinforcements to place.");

        if (allocation.Values.Any(v => v < 0))
            throw new InvalidOperationException("Cannot allocate a negative number of soldiers.");

        if (allocation.Values.Sum() != game.PendingReinforcements)
            throw new InvalidOperationException($"You must place exactly {game.PendingReinforcements} soldiers.");

        foreach (var (code, amount) in allocation)
        {
            var country = GetCountry(game, code);
            if (country.OwnerId != playerId)
                throw new InvalidOperationException($"You do not own {country.Name}.");
        }

        foreach (var (code, amount) in allocation)
            GetCountry(game, code).Soldiers += amount;

        game.PendingReinforcements = 0;
        game.Phase = TurnPhase.Action;
    }

    // Ends the turn (without attacking, or after the one attack is finished).
    public void Skip(GameEvent game, string playerId)
    {
        EnsureTurn(game, playerId);
        if (game.Phase == TurnPhase.Attacking)
            throw new InvalidOperationException("An attack is in progress. Stop it or finish it first.");
        if (game.Phase == TurnPhase.Reinforce)
            throw new InvalidOperationException("Place your reinforcements first.");

        AdvanceTurn(game);
    }

    // ---------- Attacking ----------

    public void StartAttack(GameEvent game, string playerId, string attackerCode, string defenderCode)
    {
        EnsureTurn(game, playerId);
        if (game.Phase == TurnPhase.Reinforce)
            throw new InvalidOperationException("Place your reinforcements first.");
        if (game.Phase == TurnPhase.Attacking || game.PendingAttack != null)
            throw new InvalidOperationException("An attack is already in progress.");
        if (game.AttackUsedThisTurn)
            throw new InvalidOperationException("You have already attacked this turn.");

        var attacker = GetCountry(game, attackerCode);
        var defender = GetCountry(game, defenderCode);

        if (attacker.OwnerId != playerId)
            throw new InvalidOperationException("You can only attack from a country you own.");
        if (defender.OwnerId == playerId)
            throw new InvalidOperationException("You cannot attack your own country.");
        if (attacker.Soldiers < 2)
            throw new InvalidOperationException("You need at least 2 soldiers to attack.");

        game.PendingAttack = new PendingAttack
        {
            AttackerCode = attackerCode,
            DefenderCode = defenderCode,
            StartedAt = DateTime.UtcNow
        };
        game.Phase = TurnPhase.Attacking;
    }

    // One round of dice for the pending attack.
    public RoundResult ResolveDiceRound(GameEvent game)
    {
        if (game.Status != GameStatus.InProgress)
            throw new InvalidOperationException("The game is not in progress.");
        if (game.PendingAttack == null || game.Phase != TurnPhase.Attacking)
            throw new InvalidOperationException("There is no attack in progress.");

        var attacker = GetCountry(game, game.PendingAttack.AttackerCode);
        var defender = GetCountry(game, game.PendingAttack.DefenderCode);

        if (attacker.Soldiers < 2)
            throw new InvalidOperationException("The attacker needs at least 2 soldiers to continue.");

        // Each side rolls one die per soldier fighting.
        int dice = Math.Min(attacker.Soldiers - 1, defender.Soldiers);

        var result = new RoundResult();
        for (int i = 0; i < dice; i++) result.AttackerDice.Add(_dice.Roll());
        for (int i = 0; i < dice; i++) result.DefenderDice.Add(_dice.Roll());

        for (int i = 0; i < dice; i++)
        {
            if (result.AttackerDice[i] > result.DefenderDice[i]) result.DefenderLosses++;
            else if (result.AttackerDice[i] < result.DefenderDice[i]) result.AttackerLosses++;
            // equal: nobody loses a soldier
        }

        attacker.Soldiers -= result.AttackerLosses;
        defender.Soldiers -= result.DefenderLosses;

        result.AttackerSoldiersAfter = attacker.Soldiers;
        result.DefenderSoldiersAfter = defender.Soldiers;

        if (defender.Soldiers <= 0)
        {
            var attackerOwnerId = attacker.OwnerId;
            var defenderOwnerId = defender.OwnerId;

            result.SoldiersGained = Conquer(game, attacker, defender);
            result.Conquered = true;
            result.AttackEnded = true;

            UpdateEliminations(game);
            if (GetPlayer(game, defenderOwnerId).Eliminated)
                result.EliminatedPlayerId = defenderOwnerId;

            if (CheckGameOver(game))
            {
                result.GameOver = true;
                result.WinnerId = game.WinnerId;
            }
        }
        else if (attacker.Soldiers <= 1)
        {
            // Attacker is out of fighters: attack fails, defender keeps its country.
            EndAttack(game);
            result.AttackEnded = true;
        }

        return result;
    }

    // The player stops the attack. Their turn ends.
    public void StopAttack(GameEvent game, string playerId)
    {
        EnsureTurn(game, playerId);
        if (game.Phase != TurnPhase.Attacking || game.PendingAttack == null)
            throw new InvalidOperationException("There is no attack to stop.");

        game.PendingAttack = null;
        AdvanceTurn(game);
    }

    // Defender changes owner and is garrisoned with the conquest bonus. Returns the bonus.
    private int Conquer(GameEvent game, CountryState attacker, CountryState defender)
    {
        int gained = Math.Max(2, defender.StartingSoldiers / 2);

        defender.OwnerId = attacker.OwnerId;
        defender.Soldiers = gained;

        EndAttack(game);
        return gained;
    }

    // Attack finished (not stopped): the player may still transfer, then ends the turn.
    private static void EndAttack(GameEvent game)
    {
        game.PendingAttack = null;
        game.AttackUsedThisTurn = true;
        game.Phase = TurnPhase.Action;
    }

    // ---------- Moving soldiers ----------

    public void Transfer(GameEvent game, string playerId, string fromCode, string toCode, int amount)
    {
        EnsureTurn(game, playerId);
        if (game.Phase == TurnPhase.Attacking)
            throw new InvalidOperationException("You cannot transfer soldiers during an attack.");
        if (game.Phase == TurnPhase.Reinforce)
            throw new InvalidOperationException("Place your reinforcements first.");
        if (amount <= 0)
            throw new InvalidOperationException("Transfer at least 1 soldier.");
        if (fromCode == toCode)
            throw new InvalidOperationException("Choose two different countries.");

        var from = GetCountry(game, fromCode);
        var to = GetCountry(game, toCode);

        if (from.OwnerId != playerId || to.OwnerId != playerId)
            throw new InvalidOperationException("You can only transfer between countries you own.");
        if (from.Soldiers - amount < 1)
            throw new InvalidOperationException("A country must keep at least 1 soldier.");

        from.Soldiers -= amount;
        to.Soldiers += amount;
    }

    // ---------- Checks ----------

    public void UpdateEliminations(GameEvent game)
    {
        foreach (var player in game.Players)
            player.Eliminated = !game.Countries.Any(c => c.OwnerId == player.Id);
    }

    // Winner = one player owns all countries.
    public string? CheckWinner(GameEvent game)
    {
        var firstOwner = game.Countries[0].OwnerId;
        return game.Countries.All(c => c.OwnerId == firstOwner) ? firstOwner : null;
    }

    // Finishes the game if someone has conquered the world, or the human has been eliminated.
    private bool CheckGameOver(GameEvent game)
    {
        var winner = CheckWinner(game);
        if (winner != null)
        {
            game.WinnerId = winner;
            game.Status = GameStatus.Finished;
            return true;
        }

        var human = game.Players.First(p => p.Type == PlayerType.Human);
        if (human.Eliminated)
        {
            game.WinnerId = null;
            game.Status = GameStatus.Finished;
            return true;
        }

        return false;
    }

    // ---------- Helpers ----------

    private static Player GetPlayer(GameEvent game, string playerId) =>
        game.Players.First(p => p.Id == playerId);

    private static CountryState GetCountry(GameEvent game, string code) =>
        game.Countries.FirstOrDefault(c => c.Code == code)
        ?? throw new InvalidOperationException($"Unknown country '{code}'.");

    private static IEnumerable<CountryState> OwnedBy(GameEvent game, string playerId) =>
        game.Countries.Where(c => c.OwnerId == playerId);

    private static void EnsureTurn(GameEvent game, string playerId)
    {
        if (game.Status != GameStatus.InProgress)
            throw new InvalidOperationException("The game is not in progress.");
        if (game.TurnOrder[game.CurrentTurnIndex] != playerId)
            throw new InvalidOperationException("It is not this player's turn.");
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
