using UCTWebGame.Engine;
using UCTWebGame.Models;

namespace UCTWebGame.DTOs;

// Game        = the final game state after the action and the NPC turns
// RoundResult = dice of the human's own roll (if the action was a roll)
// Commentary  = commentary on the human's roll
// Log         = what the NPCs did after the action, in order (played back by the React page)
// Baseline    = every country's owner/soldiers right before the first NPC move, so the page can replay the log
public sealed record GameActionResponse(
    GameEvent Game,
    RoundResult? RoundResult = null,
    string? Commentary = null,
    IReadOnlyList<GameLogEntry>? Log = null,
    IReadOnlyList<CountryChange>? Baseline = null);