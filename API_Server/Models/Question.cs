using System.ComponentModel.DataAnnotations;

namespace API_Server.Models
{
    public class Question
    {
        [Required]
        public string Topic { get; set; }
        [Required]
        public string Content { get; set; }
        [Required]
        public string CorrectAnswer { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Owner { get; set; }
    }
}
