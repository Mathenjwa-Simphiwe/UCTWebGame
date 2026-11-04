using MongoDB.Bson;
using MongoDB.Driver;
using UCTWebGame.Models;

namespace UCTWebGame.Services;

public sealed class MongoDbService
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<User> Users { get; }
    public IMongoCollection<GameEvent> Games { get; }

    public MongoDbService(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDb:ConnectionString"];
        var databaseName = configuration["MongoDb:DatabaseName"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("MongoDb:ConnectionString is not configured.");
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("MongoDb:DatabaseName is not configured.");

        var client = new MongoClient(connectionString);
        Database = client.GetDatabase(databaseName);
        Users = Database.GetCollection<User>("users");
        Games = Database.GetCollection<GameEvent>("games");
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var emailIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(user => user.Email),
            new CreateIndexOptions { Unique = true, Name = "uq_users_email" });

        var gameOwnerIndex = new CreateIndexModel<GameEvent>(
            Builders<GameEvent>.IndexKeys
                .Ascending(game => game.UserId)
                .Descending(game => game.UpdatedAt),
            new CreateIndexOptions { Name = "ix_games_user_updated" });

        await Users.Indexes.CreateOneAsync(emailIndex, cancellationToken: cancellationToken);
        await Games.Indexes.CreateOneAsync(gameOwnerIndex, cancellationToken: cancellationToken);
    }
}
