using MailKit.Net.Smtp;
using MimeKit;

namespace API_Server.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration) => _configuration = configuration;

        public void SendOtpEmail(string email, string otp)
        {
            var emailFrom = _configuration["EmailSettings:Email"];
            var emailPassword = _configuration["EmailSettings:Password"];
            MimeMessage email_Message = new MimeMessage();
            MailboxAddress email_From = new MailboxAddress("Net_Study", emailFrom);
            email_Message.From.Add(email_From);

            MailboxAddress email_To = new MailboxAddress("My guest", email);
            email_Message.To.Add(email_To);

            email_Message.Subject = "Your OTP code";
            BodyBuilder emailBodyBuilder = new BodyBuilder();
            emailBodyBuilder.TextBody = $"Your OTP code is: {otp}";
            email_Message.Body = emailBodyBuilder.ToMessageBody();

            using (SmtpClient MailClient = new SmtpClient())
            {
                MailClient.Connect("smtp.gmail.com", 465, true);
                MailClient.Authenticate(emailFrom, emailPassword);
                MailClient.Send(email_Message);
                MailClient.Disconnect(true);
            }
        }
    }
}
