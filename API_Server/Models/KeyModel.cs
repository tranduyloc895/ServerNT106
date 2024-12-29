using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace API_Server.Models
{
    public class KeyModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? Username1 { get; set; }
        public string? Key { get; set; }
    }
}
