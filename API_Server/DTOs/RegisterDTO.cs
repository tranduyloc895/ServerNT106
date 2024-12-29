using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace API_Server.DTOs
{
    public class RegisterDTO
    {
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? Username { get; set; }

        [Required]
        public string? Email { get; set; }
        [Required]
        public string? Password { get; set; }
        [Required]
        public string? ConfirmPassword { get; set; }
        public string  PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string Salt { get; set; }
        public DateTime DateOfBirth { get; set; }
    }


}
