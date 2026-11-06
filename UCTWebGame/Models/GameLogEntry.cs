using UCTWebGame.Engine;

namespace UCTWebGame.Models;

// What each soldier count / owner looks like AFTER an event. The React page applies these
// one by one so the map changes while the log plays back.
public class CountryChange
{
    public string Code { get; set; } = string.Empty;
    public int Soldiers { get; set; }
    public string OwnerId { get; set; } = string.Empty;
}

public static class LogTypes
{
    public const string Reinforce = "Reinforce";
    public const string AttackStart = "AttackStart";
    public const string DiceRound = "DiceRound";
    public const string Conquest = "Conquest";
    public const string AttackFailed = "AttackFailed";
    public const string Retreat = "Retreat";
    public const string Transfer = "Transfer";
    public const string EndTurn = "EndTurn";
    public const string Eliminated = "Eliminated";
    public const string GameOver = "GameOver";
}

// One line of the battle log: what an NPC did, why, and (for dice rounds) the dice.
public class GameLogEntry
{
    public int Seq { get; set; }
    public int Round { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Justification { get; set; }     // the NPC's reason, built by the strategy code
    public string? Commentary { get; set; }        // LLM (or template) commentary on dice rounds
    public string? AttackerCode { get; set; }
    public string? DefenderCode { get; set; }
    public RoundResult? Result { get; set; }       // dice, losses, outcome
    public List<CountryChange> Changes { get; set; } = new();
}
