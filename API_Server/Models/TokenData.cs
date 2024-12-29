using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace API_Server.Models
{
    public class TokenData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public string? Username { get; set; }
        public string Jti { get; set; } = string.Empty;
        public List<string> RefreshTokensUsed { get; set; } = new List<string>();
    }
}
