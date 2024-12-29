using API_Server.Models;

namespace API_Server.DTOs
{
    public class QuestionDTO
    {
        public string Topic { get; set; }
        public string Content { get; set; }
        public string CorrectAnswer { get; set; }

    }
}
