using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UCTWebGame.Models;

public class GameEvent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public GameStatus Status { get; set; } = GameStatus.Setup;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Incremented by the persistence service when a saved game changes.
    public int Version { get; set; } = 1;

    public int Round { get; set; } = 1;
    public List<string> TurnOrder { get; set; } = new();
    public int CurrentTurnIndex { get; set; }

    [BsonRepresentation(BsonType.String)]
    public TurnPhase Phase { get; set; } = TurnPhase.Action;

    public int PendingReinforcements { get; set; }
    public List<Player> Players { get; set; } = new();
    public List<CountryState> Countries { get; set; } = new();
    public PendingAttack? PendingAttack { get; set; }
    public bool AttackUsedThisTurn { get; set; }
    public string? WinnerId { get; set; }
    public int LastSequence { get; set; }

    // Battle log (latest entries only). Shown on the battle page and to anyone watching.
    public List<GameLogEntry> Log { get; set; } = new();
}