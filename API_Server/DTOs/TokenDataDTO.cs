using MongoDB.Bson;

namespace API_Server.DTOs
{
    public class TokenDataDTO
    {
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string? Username { get; set; }
    }
}
