using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace API_Server.Models
{
    public class UserTask
    {
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool? IsCompleted { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Owner { get; set; }
    }
}
