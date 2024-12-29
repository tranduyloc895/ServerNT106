using API_Server.DTOs;
using API_Server.Models;
using API_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API_Server.Controllers
{
    [Route("api/chat")]
    [ApiController]
    public class SingleChatController : ControllerBase
    {
        private readonly SingleChatService _chatService;

        public SingleChatController(SingleChatService chatService)
        {
            _chatService = chatService;
        }

        // API to send a message
        [Authorize]
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessage chatMessage)
        {
            try
            {
                if (chatMessage == null || string.IsNullOrEmpty(chatMessage.Sender) || string.IsNullOrEmpty(chatMessage.Receiver) || string.IsNullOrEmpty(chatMessage.Content))
                {
                    return BadRequest(new
                    {
                        message = "Tin nhắn không hợp lệ"
                    });
                }
                var msg = new SingleChat
                {
                    Sender = chatMessage.Sender,
                    Receiver = chatMessage.Receiver,
                    Content = chatMessage.Content,
                    SessionKeyEncrypted = chatMessage.SessionKeyEncrypted,
                    Timestamp = DateTime.UtcNow
                };
                await _chatService.SendMessageAsync(msg);

                return Ok(new
                {
                    message = "Gửi tin nhắn thành công",
                    contentMsg = msg
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message,
                });
            }
        }

        // API to get chat history
        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory([FromQuery] string user1, [FromQuery] string user2)
        {
            var messages = await _chatService.GetMessagesAsync(user1, user2);
            return Ok(new
            {
                message = "Lấy tin nhắn thành công!",
                data = messages
            });
        }
    }
}