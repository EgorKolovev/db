using System;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoUserRepository : IUserRepository
    {
        private readonly IMongoCollection<UserEntity> userCollection;
        public const string CollectionName = "users";

        public MongoUserRepository(IMongoDatabase database)
        {
            ArgumentNullException.ThrowIfNull(database);
            userCollection = database.GetCollection<UserEntity>(CollectionName);
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            var keys = Builders<UserEntity>.IndexKeys.Ascending(u => u.Login);
            var options = new CreateIndexOptions { Name = "idx_users_login", Unique = true };
            userCollection.Indexes.CreateOne(new CreateIndexModel<UserEntity>(keys, options));
        }

        public UserEntity Insert(UserEntity user)
        {
            ArgumentNullException.ThrowIfNull(user);
            var id = user.Id == Guid.Empty ? Guid.NewGuid() : user.Id;
            var entity = new UserEntity(id, user.Login, user.LastName, user.FirstName, user.GamesPlayed, user.CurrentGameId);
            userCollection.InsertOne(entity);
            return entity;
        }

        public UserEntity FindById(Guid id)
        {
            var filter = Builders<UserEntity>.Filter.Eq(u => u.Id, id);
            return userCollection.Find(filter).FirstOrDefault();
        }

        public UserEntity GetOrCreateByLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentException("Login must be provided", nameof(login));

            var filter = Builders<UserEntity>.Filter.Eq(u => u.Login, login);
            var update = Builders<UserEntity>.Update
                .SetOnInsert(u => u.Id, Guid.NewGuid())
                .SetOnInsert(u => u.Login, login)
                .SetOnInsert(u => u.FirstName, string.Empty)
                .SetOnInsert(u => u.LastName, string.Empty)
                .SetOnInsert(u => u.GamesPlayed, 0)
                .SetOnInsert(u => u.CurrentGameId, (Guid?)null);

            var options = new FindOneAndUpdateOptions<UserEntity>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            return userCollection.FindOneAndUpdate(filter, update, options);
        }

        public void Update(UserEntity user)
        {
            ArgumentNullException.ThrowIfNull(user);
            if (user.Id == Guid.Empty)
                throw new ArgumentException("User must have non-empty id for update", nameof(user));

            var filter = Builders<UserEntity>.Filter.Eq(u => u.Id, user.Id);
            userCollection.ReplaceOne(filter, user, new ReplaceOptions { IsUpsert = false });
        }

        public void Delete(Guid id)
        {
            var filter = Builders<UserEntity>.Filter.Eq(u => u.Id, id);
            userCollection.DeleteOne(filter);
        }

        public PageList<UserEntity> GetPage(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageNumber));
            if (pageSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageSize));

            var filter = FilterDefinition<UserEntity>.Empty;
            var totalCount = userCollection.CountDocuments(filter);
            var items = userCollection
                .Find(filter)
                .SortBy(u => u.Login)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            return new PageList<UserEntity>(items, totalCount, pageNumber, pageSize);
        }

        public void UpdateOrInsert(UserEntity user, out bool isInserted)
        {
            throw new NotSupportedException("Use Update or Insert explicitly.");
        }
    }
}
