using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UCTWebGame.Models
{
    public class GameAnayltics
    {
        // Append-only log: powers the battle feed, NPC justifications, replay and analytics.
public class GameEvent
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string? Id { get; set; }

            [BsonRepresentation(BsonType.ObjectId)]
            public string GameId { get; set; } = string.Empty;

            public int Seq { get; set; }                       // ordering within the game
            public int Round { get; set; }
            public string PlayerId { get; set; } = string.Empty;

            [BsonRepresentation(BsonType.String)]
            public GameEventType Type { get; set; }

            [BsonRepresentation(BsonType.String)]
            public EventSeverity Severity { get; set; } = EventSeverity.Info;

            public DateTime Timestamp { get; set; } = DateTime.UtcNow;

            public EventData? Data { get; set; }

            public string? Commentary { get; set; }            // LLM (or fallback) text
            public string? Justification { get; set; }         // NPC reasoning, built in C#
        }
    }
}
