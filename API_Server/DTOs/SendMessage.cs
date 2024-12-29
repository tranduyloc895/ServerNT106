namespace API_Server.DTOs
{
    public class SendMessage
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Content { get; set; }
        public Dictionary<string, string> SessionKeyEncrypted { get; set; } = new Dictionary<string, string>();
    }
}
