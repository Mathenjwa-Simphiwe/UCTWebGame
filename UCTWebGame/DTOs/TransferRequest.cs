using System.ComponentModel.DataAnnotations;

namespace UCTWebGame.DTOs;

public sealed class TransferRequest
{
    [Required]
    public string FromCode { get; set; } = string.Empty;

    [Required]
    public string ToCode { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Amount { get; set; }
}
