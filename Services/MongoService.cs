using LocationTrackerAPI.Models;
using MongoDB.Driver;

namespace LocationTrackerAPI.Services
{
    public class MongoService
    {
        private readonly IMongoDatabase _db;

        public MongoService(IConfiguration config)
        {
            var client = new MongoClient(config["MongoDbSettings:ConnectionString"]);
            _db = client.GetDatabase(config["MongoDbSettings:DatabaseName"]);
        }

        public IMongoCollection<User> Users => _db.GetCollection<User>("users");
        public IMongoCollection<Location> Locations => _db.GetCollection<Location>("locations");
    }
}
