using MongoDB.Bson.Serialization.Attributes;

namespace UCTWebGame.Models;

// Static seed data: one document per real-world country (the "countries" collection).
public class CountryInfo
{
    [BsonId]
    public string Code { get; set; } = string.Empty;   // e.g. "ZA"

    public string Name { get; set; } = string.Empty;
    public long Population { get; set; }
    public int StartingSoldiers { get; set; }          // computed from the population table
}