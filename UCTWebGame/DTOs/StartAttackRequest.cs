using System.ComponentModel.DataAnnotations;

namespace UCTWebGame.DTOs;

public sealed class StartAttackRequest
{
    [Required]
    public string AttackerCode { get; set; } = string.Empty;

    [Required]
    public string DefenderCode { get; set; } = string.Empty;
}
