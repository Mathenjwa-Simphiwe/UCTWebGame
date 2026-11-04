using System.ComponentModel.DataAnnotations;

namespace UCTWebGame.DTOs;

public sealed class StartGameRequest
{
    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string ChosenCountryCode { get; set; } = string.Empty;
}
