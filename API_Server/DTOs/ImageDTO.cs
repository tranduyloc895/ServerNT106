using System.ComponentModel.DataAnnotations;

namespace API_Server.DTOs
{
    public class ImageDTO
    {
        [Required(ErrorMessage = "Please choose an image!")]
        public IFormFile file { get; set; }
    }
}
