using System.ComponentModel.DataAnnotations;

namespace UCTWebGame.DTOs;

public sealed class ApplyReinforcementsRequest
{
    [Required]
    public Dictionary<string, int> Allocation { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
