namespace API_Server.Models
{
    public class ChangePasswordRequest
    {
        public string Username { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
        public string Otp { get; set; }

    }
}
