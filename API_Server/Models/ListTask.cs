using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace API_Server.Models
{
    public class ListTask
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string? Owner { get; set; }
        public List<UserTask> Tasks { get; set; }
    }
}
