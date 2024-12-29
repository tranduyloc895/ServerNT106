using Microsoft.AspNetCore.Mvc;
using API_Server.Services;
using Microsoft.AspNetCore.Authorization;
using API_Server.Models;
using static API_Server.Models.ChatBot;

namespace API_Server.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class ChatBotController : ControllerBase
    {
        private readonly ChatBotService _chatBotService;
        private readonly JwtService _jwtService;

        public ChatBotController(ChatBotService chatBotService, JwtService jwtService)
        {
            _chatBotService = chatBotService;
            _jwtService = jwtService;
        }

        [Authorize]
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (!_jwtService.IsValidate(authorizationHeader))
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }

            try
            {
                if (string.IsNullOrEmpty(request.Message))
                {
                    return BadRequest(new { message = "Nội dung không được trống!" });
                }

                var response = await _chatBotService.GetResponseAsync(request.Message);

                return Ok(new
                {
                    message = "Lấy phản hồi thành công!",
                    response = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

    }
}
