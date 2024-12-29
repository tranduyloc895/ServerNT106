using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace API_Server.Models
{
    public class SingleChat
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Content { get; set; }
        public Dictionary<string, string> SessionKeyEncrypted { get; set; } = new Dictionary<string, string>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}