using System.Security.Cryptography.X509Certificates;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace UCTWebGame.Models
{
    public class GameEvent
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } 

        [BsonRepresentation(BsonType.String)]

        public string Status { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        //increments on every save
        
        //when the version matches the one that was loaded.
        public int Version { get; set; } = 1;

        //turn tracking
        public int Round { get; set; } = 1;
        
        public List<string> TurnOrder { get; set; } = new List<string>();

        public int CurrentTurns { get; set; } 

        [BsonRepresentation(BsonType.String)])

        public TurnPhase Phase { get; set; } = TurnPhase.Start;

        public int PendingReinforcements { get; set; } = 0;

        //State of the board, including the positions of all pieces, resources, and other relevant information.

        public List <Player> Players { get; set; } = new 
        PublicKey List<CountryState> Countries { get; set; } = new();

        public PendingAttack PendingAttack { get; set; } 

        public string? WinnerId { get; set; }

        //sequence for events saved in the game

        public int LastSequence { get; set; } 
    }

}
