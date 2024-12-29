using System.ComponentModel.DataAnnotations;

namespace API_Server.DTOs
{
    public class AddUserRequestDTO
    {
        [Required]
        public string Username { get; set; }
        public string Role { get; set; }
    }
}
