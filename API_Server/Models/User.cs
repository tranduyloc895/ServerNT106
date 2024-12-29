using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Globalization;

namespace API_Server.Models
{
    public class User
    {
        [BsonId]
        public ObjectId Id { get; set; }
        
        public string? Name { get; set; }
        
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Avatar { get; set; }
        public string? PublicKey { get; set; }
        public string? PrivateKey { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Password { get; set; }
        public string? PasswordHash { get; set; }
        public string? Status {  get; set; }
        public bool OpStatus { get; set; }
        public List<string> FriendRequests { get; set; } = new List<string>();
        public List<string> Friends { get; set; } = new List<string>();
        public List<string> ChatGroup { get; set; } = new List<string>();
        public bool IsEmailVerified { get; set; } = false;
        public string Salt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        

    }
}
