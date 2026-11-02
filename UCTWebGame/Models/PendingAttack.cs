namespace UCTWebGame.Models;

// Stored in the game document so an attack survives a closed browser or page refresh.
public class PendingAttack
{
    public string AttackerCode { get; set; } = string.Empty;
    public string DefenderCode { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}

