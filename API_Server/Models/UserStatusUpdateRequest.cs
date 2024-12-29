namespace API_Server.Models
{
    public class UserStatusUpdateRequest
    {
        public string Username { get; set; }
        public bool? OpStatus { get; set; }
    }
}