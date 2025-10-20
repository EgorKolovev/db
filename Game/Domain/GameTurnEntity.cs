using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace Game.Domain
{
    public class GameTurnEntity
    {
        [BsonElement("players")]
        private readonly List<GameTurnPlayer> players;

        [BsonConstructor]
        public GameTurnEntity(
            Guid id,
            Guid gameId,
            int turnIndex,
            Guid? winnerId,
            DateTime finishedAtUtc,
            List<GameTurnPlayer> players)
        {
            Id = id;
            GameId = gameId;
            TurnIndex = turnIndex;
            WinnerId = winnerId;
            FinishedAtUtc = finishedAtUtc;
            this.players = players ?? new List<GameTurnPlayer>();
        }

        public GameTurnEntity(
            Guid id,
            Guid gameId,
            int turnIndex,
            Guid? winnerId,
            DateTime finishedAtUtc,
            IEnumerable<GameTurnPlayer> players)
            : this(id, gameId, turnIndex, winnerId, finishedAtUtc, players?.ToList() ?? new List<GameTurnPlayer>())
        {
        }

        public GameTurnEntity(
            Guid gameId,
            int turnIndex,
            Guid? winnerId,
            DateTime finishedAtUtc,
            IEnumerable<GameTurnPlayer> players)
            : this(Guid.NewGuid(), gameId, turnIndex, winnerId, finishedAtUtc, players)
        {
        }

        [BsonId]
        public Guid Id { get; private set; }

        [BsonElement("gameId")]
        public Guid GameId { get; }

        [BsonElement("turnIndex")]
        public int TurnIndex { get; }

        [BsonElement("winnerId")]
        public Guid? WinnerId { get; }

        [BsonElement("finishedAtUtc")]
        public DateTime FinishedAtUtc { get; }

        [BsonIgnore]
        public IReadOnlyList<GameTurnPlayer> Players => players.AsReadOnly();
    }

    public class GameTurnPlayer
    {
        [BsonConstructor]
        public GameTurnPlayer(Guid userId, string name, PlayerDecision decision, int scoreAfterTurn)
        {
            UserId = userId;
            Name = name;
            Decision = decision;
            ScoreAfterTurn = scoreAfterTurn;
        }

        [BsonElement("userId")]
        public Guid UserId { get; }

        [BsonElement("name")]
        public string Name { get; }

        [BsonElement("decision")]
        public PlayerDecision Decision { get; }

        [BsonElement("scoreAfterTurn")]
        public int ScoreAfterTurn { get; }
    }
}
