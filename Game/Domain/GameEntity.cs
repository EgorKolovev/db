using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace Game.Domain
{
    public class GameEntity
    {
        [BsonElement("players")]
        private readonly List<Player> players;

        public GameEntity(int turnsCount)
            : this(Guid.Empty, GameStatus.WaitingToStart, turnsCount, 0, new List<Player>())
        {
        }

        [BsonConstructor]
        public GameEntity(
            Guid id,
            GameStatus status,
            int turnsCount,
            int currentTurnIndex,
            List<Player> players)
        {
            Id = id;
            Status = status;
            TurnsCount = turnsCount;
            CurrentTurnIndex = currentTurnIndex;
            this.players = players ?? new List<Player>();
        }

        public Guid Id
        {
            get;
            // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local For MongoDB
            private set;
        }

        [BsonIgnore]
        public IReadOnlyList<Player> Players => players.AsReadOnly();

        [BsonElement("turnsCount")]
        public int TurnsCount { get; }

        [BsonElement("currentTurnIndex")]
        public int CurrentTurnIndex { get; private set; }

        [BsonElement("status")]
        public GameStatus Status { get; private set; }

        public void AddPlayer(UserEntity user)
        {
            if (Status != GameStatus.WaitingToStart)
                throw new ArgumentException(Status.ToString());
            players.Add(new Player(user.Id, user.Login));
            if (Players.Count == 2)
                Status = GameStatus.Playing;
        }

        public bool IsFinished()
        {
            return CurrentTurnIndex >= TurnsCount
                   || Status == GameStatus.Finished
                   || Status == GameStatus.Canceled;
        }

        public void Cancel()
        {
            if (!IsFinished())
                Status = GameStatus.Canceled;
        }

        public bool HaveDecisionOfEveryPlayer => Players.All(p => p.Decision.HasValue);

        public void SetPlayerDecision(Guid userId, PlayerDecision decision)
        {
            if (Status != GameStatus.Playing)
                throw new InvalidOperationException(Status.ToString());
            foreach (var player in Players.Where(p => p.UserId == userId))
            {
                if (player.Decision.HasValue)
                    throw new InvalidOperationException(player.Decision.ToString());
                player.Decision = decision;
            }
        }

        public GameTurnEntity FinishTurn()
        {
            if (players.Count != 2)
                throw new InvalidOperationException("Expected exactly two players to finish turn.");

            Guid? winnerId = null;
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var opponent = players[1 - i];
                if (!player.Decision.HasValue || !opponent.Decision.HasValue)
                    throw new InvalidOperationException("All players must have decisions to finish the turn.");
                if (player.Decision.Value.Beats(opponent.Decision.Value))
                {
                    player.Score++;
                    winnerId = player.UserId;
                }
            }

            var turnIndex = CurrentTurnIndex;
            var turnPlayers = players
                .Select(player =>
                {
                    if (!player.Decision.HasValue)
                        throw new InvalidOperationException("Missing decision when building turn snapshot.");
                    return new GameTurnPlayer(
                        player.UserId,
                        player.Name,
                        player.Decision.Value,
                        player.Score);
                })
                .ToList();

            var result = new GameTurnEntity(
                Guid.NewGuid(),
                Id,
                turnIndex,
                winnerId,
                DateTime.UtcNow,
                turnPlayers);

            foreach (var player in players)
                player.Decision = null;
            CurrentTurnIndex++;
            if (CurrentTurnIndex == TurnsCount)
                Status = GameStatus.Finished;
            return result;
        }
    }
}
