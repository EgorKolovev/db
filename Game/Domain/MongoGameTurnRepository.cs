using System;
using System.Collections.Generic;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoGameTurnRepository : IGameTurnRepository
    {
        public const string CollectionName = "game-turns";
        private readonly IMongoCollection<GameTurnEntity> collection;

        public MongoGameTurnRepository(IMongoDatabase database)
        {
            ArgumentNullException.ThrowIfNull(database);
            collection = database.GetCollection<GameTurnEntity>(CollectionName);
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            var keys = Builders<GameTurnEntity>.IndexKeys
                .Ascending(t => t.GameId)
                .Descending(t => t.TurnIndex);
            var options = new CreateIndexOptions { Name = "idx_game_turns_game_turn", Unique = true };
            collection.Indexes.CreateOne(new CreateIndexModel<GameTurnEntity>(keys, options));
        }

        public void Insert(GameTurnEntity turn)
        {
            ArgumentNullException.ThrowIfNull(turn);
            collection.InsertOne(turn);
        }

        public IReadOnlyList<GameTurnEntity> GetLastTurns(Guid gameId, int limit)
        {
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit));

            var filter = Builders<GameTurnEntity>.Filter.Eq(t => t.GameId, gameId);
            return collection
                .Find(filter)
                .SortByDescending(t => t.TurnIndex)
                .Limit(limit)
                .ToList();
        }
    }
}
