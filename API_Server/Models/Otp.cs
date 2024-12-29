using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace API_Server.Models
{
    public class Otp
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
    }

    public class OtpRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}
