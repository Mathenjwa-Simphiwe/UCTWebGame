namespace UCTWebGame.Models;

// Optional details for an event. Only the fields relevant to that event type are filled in.
public class EventData
{
    // Attacks, dice rounds, conquests
    public string? AttackerCode { get; set; }
    public string? DefenderCode { get; set; }
    public List<int>? AttackerDice { get; set; }
    public List<int>? DefenderDice { get; set; }
    public int? AttackerLosses { get; set; }
    public int? DefenderLosses { get; set; }
    public int? AttackerSoldiersAfter { get; set; }
    public int? DefenderSoldiersAfter { get; set; }

    // Conquest
    public int? SoldiersGained { get; set; }

    // Reinforcements and transfers
    public string? FromCountryCode { get; set; }
    public string? ToCountryCode { get; set; }
    public int? Amount { get; set; }

    // NPC decisions: "attack", "skip", "stop", "reinforce", "transfer"
    public string? Action { get; set; }
}