namespace API_Server.DTOs
{
    public class UserDTO
    {
        public string? Name { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PublicKey { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Status { get; set; }
        public bool OpStatus { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
