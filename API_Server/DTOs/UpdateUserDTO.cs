using System.ComponentModel.DataAnnotations;

namespace API_Server.DTOs
{
    public class UpdateUserDTO
    {
        public string Name { get; set; }
      
        public string Username { get; set; }
        
        public string Email { get; set; }

        public IFormFile Avatar { get; set; }

    }
}
