using API_Server.Models;
using API_Server.Services;
using Microsoft.AspNetCore.SignalR;
using NetStudy.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API_Server.Hubs
{
    public class ChatHub : Hub
    {
        private readonly SingleChatService _chatService;
        private readonly UserService _userService;
        private readonly RsaService _rsaService;
        private readonly AesService _aesService;
        public ChatHub(SingleChatService chatService, UserService userService, RsaService rsaService, AesService aesService)
        {
            _chatService = chatService;
            _userService = userService;
            _rsaService = rsaService;
            _aesService = aesService;
        }

        public override async Task OnConnectedAsync()
        {
            var username = Context.GetHttpContext()?.Request.Query["username"].ToString();
            if (!string.IsNullOrEmpty(username))
            {
                Context.Items["User"] = username;
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.GetHttpContext()?.Request.Query["username"].ToString();
            if (!string.IsNullOrEmpty(username))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, username);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string senderName, string receiverName, string message, string senderKey, string receiverKey)
        {
            var chatMessage = new SingleChat
            {
                Sender = senderName,
                Receiver = receiverName,
                Content = message,
                SessionKeyEncrypted = new Dictionary<string, string>
                {
                    {senderName, senderKey},
                    {receiverName, receiverKey }
                },
                Timestamp = DateTime.UtcNow
            };
           

            // Gửi tin nhắn cho cả người gửi và người nhận
            await Clients.Group(senderName).SendAsync("ReceiveMessage", senderName, chatMessage);
            await Clients.Group(receiverName).SendAsync("ReceiveMessage", senderName, chatMessage);
            await _chatService.SendMessageAsync(chatMessage);
        }

        public async Task<List<SingleChat>> GetChatHistory(string user1, string user2)
        {
            return await _chatService.GetMessagesAsync(user1, user2);
        }

        public async Task UpdateStatus(string username, string status)
        {
            await Clients.All.SendAsync("ReceiveStatusUpdate", username, status);
        }
    }
}