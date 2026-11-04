using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UCTWebGame.Models;

public class Player
{
    // Player IDs are game-local values such as p1 and p2, not MongoDB ObjectIds.
    public string Id { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public PlayerType Type { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Reinforcements { get; set; }
    public string StartCountry { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool Eliminated { get; set; }
}
