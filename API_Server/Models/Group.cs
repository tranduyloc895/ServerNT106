using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Server.Models
{
    public class Group
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } 
        public string Name { get; set; }
        public string Description { get; set; }
        public string Creator { get; set; }

        // Mảng lưu các thành viên (username và role) trong group
        public List<MemberRole> Members { get; set; } = new List<MemberRole>();
        public string GroupKey { get; set; } // Aes key riêng của nhóm
        public List<string> MemberRequest { get; set; } = new List<string>();
        public Dictionary<string, string> SessionKeyEncrypted { get; set; } = new Dictionary<string, string>();
    }

}
