using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UCTWebGame.Models
{
    public class Player
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public PlayerType { get; set; }
        
        public string Name { get; set; }
        public int Reinforcements { get; set; } = 0;
        public string StartCountry { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool Eliminated { get; set; } 
    }

}