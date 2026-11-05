using System.ComponentModel.DataAnnotations;

namespace UCTWebGame.DTOs;

public sealed class StartGameRequest
{
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string ChosenCountryCode { get; set; } = string.Empty;
}
