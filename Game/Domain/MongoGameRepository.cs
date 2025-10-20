using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoGameRepository : IGameRepository
    {
        public const string CollectionName = "games";
        private readonly IMongoCollection<GameEntity> gameCollection;

        public MongoGameRepository(IMongoDatabase db)
        {
            ArgumentNullException.ThrowIfNull(db);
            gameCollection = db.GetCollection<GameEntity>(CollectionName);
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            var keys = Builders<GameEntity>.IndexKeys.Ascending(g => g.Status);
            var options = new CreateIndexOptions { Name = "idx_games_status" };
            gameCollection.Indexes.CreateOne(new CreateIndexModel<GameEntity>(keys, options));
        }

        public GameEntity Insert(GameEntity game)
        {
            ArgumentNullException.ThrowIfNull(game);
            var id = game.Id == Guid.Empty ? Guid.NewGuid() : game.Id;
            var toInsert = new GameEntity(
                id,
                game.Status,
                game.TurnsCount,
                game.CurrentTurnIndex,
                ClonePlayers(game.Players));
            gameCollection.InsertOne(toInsert);
            return toInsert;
        }

        public GameEntity FindById(Guid gameId)
        {
            var filter = Builders<GameEntity>.Filter.Eq(g => g.Id, gameId);
            return gameCollection.Find(filter).FirstOrDefault();
        }

        public void Update(GameEntity game)
        {
            ArgumentNullException.ThrowIfNull(game);
            if (game.Id == Guid.Empty)
                throw new ArgumentException("Game must have non-empty id for update", nameof(game));

            var filter = Builders<GameEntity>.Filter.Eq(g => g.Id, game.Id);
            gameCollection.ReplaceOne(filter, game, new ReplaceOptions { IsUpsert = false });
        }

        public IList<GameEntity> FindWaitingToStart(int limit)
        {
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit));

            var filter = Builders<GameEntity>.Filter.Eq(g => g.Status, GameStatus.WaitingToStart);
            return gameCollection
                .Find(filter)
                .Limit(limit)
                .ToList();
        }

        public bool TryUpdateWaitingToStart(GameEntity game)
        {
            ArgumentNullException.ThrowIfNull(game);
            if (game.Id == Guid.Empty)
                return false;

            var filter = Builders<GameEntity>.Filter.And(
                Builders<GameEntity>.Filter.Eq(g => g.Id, game.Id),
                Builders<GameEntity>.Filter.Eq(g => g.Status, GameStatus.WaitingToStart));

            var result = gameCollection.ReplaceOne(filter, game, new ReplaceOptions { IsUpsert = false });
            return result.IsAcknowledged && result.ModifiedCount == 1;
        }

        private static List<Player> ClonePlayers(IEnumerable<Player> players)
        {
            if (players == null)
                return new List<Player>();

            return players
                .Select(player => new Player(player.UserId, player.Name)
                {
                    Decision = player.Decision,
                    Score = player.Score
                })
                .ToList();
        }
    }
}
