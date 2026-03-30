
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LocationTrackerAPI.Models
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    public class Location
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string? UserId { get; set; }
        public string? Username { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public double Speed { get; set; }   // ✅ NEW

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
