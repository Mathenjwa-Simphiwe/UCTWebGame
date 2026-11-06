using UCTWebGame.Engine;
using UCTWebGame.Models;

namespace UCTWebGame.DTOs;

public sealed record GameActionResponse(GameEvent Game, RoundResult? RoundResult = null, string? Commentary = null);
