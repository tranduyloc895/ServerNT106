namespace API_Server.Models
{
    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string Otp { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class ForgetPasswordRequest
    {
        public string Email { get; set; }
    }
}
