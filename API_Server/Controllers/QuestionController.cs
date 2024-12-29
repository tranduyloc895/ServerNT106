using API_Server.DTOs;
using API_Server.Models;
using API_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Server.Controllers
{
    [Route("api/questions")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly QuestionService _questionService;
        private readonly JwtService _jwtService;

        public QuestionController(QuestionService questionService, JwtService jwtService)
        {
            _questionService = questionService;
            _jwtService = jwtService;
        }

        [Authorize]
        [HttpPost("{username}/create-question")]
        public async Task<IActionResult> CreateQuestion([FromBody] Question question, [FromRoute] string username)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (!_jwtService.IsValidate(authorizationHeader))
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }

            if (await _questionService.IsContentExistsAsync(question.Content, username))
            {
                return BadRequest(new
                {
                    message = "Tiêu đề câu hỏi đã tồn tại."
                });
            }

            try
            {
                var createdQuestion = await _questionService.CreateQuestionAsync(question, username);

                return Ok(new
                {
                    message = "Tạo câu hỏi thành công!",
                    info = createdQuestion
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("{username}/get-question/{content}")]
        public async Task<IActionResult> GetQuestion([FromRoute] string content, [FromRoute] string username)
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
                var question = await _questionService.GetQuestionAsync(content, username);
                if (question == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy câu hỏi!"
                    });
                }

                return Ok(new
                {
                    message = "Question found!",
                    info = question
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("{username}/get-random-question")]
        public async Task<IActionResult> GetRandomQuestion([FromRoute] string username)
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
                var question = await _questionService.GetRandomQuestionAsync(username);
                if (question == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy câu hỏi!"
                    });
                }

                return Ok(new
                {
                    message = "Lấy câu hỏi ngẫu nhiên thành công!",
                    info = question
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("{username}/get-all-questions")]
        public async Task<IActionResult> GetAllQuestions([FromRoute] string username)
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
                var questions = await _questionService.GetAllQuestionsAsync(username);
                if (questions == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy câu hỏi!"
                    });
                }

                return Ok(new
                {
                    message = "Lấy danh sách câu hỏi thành công!",
                    info = questions
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("{username}/get-correct-answer/{content}")]
        public async Task<IActionResult> GetCorrectAnswer([FromRoute] string username, [FromRoute] string content)
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
                var correctAnswer = await _questionService.GetCorrectAnswer(content, username);
                if (correctAnswer == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy câu trả lời!"
                    });
                }

                return Ok(new
                {
                    message = "Correct answer found!",
                    info = correctAnswer
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("{username}/get-random-question/{topic}")]
        public async Task<IActionResult> GetRandomQuestionByTopic([FromRoute] string username, [FromRoute] string topic)
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
                var question = await _questionService.GetRandomQuestionByTopic(username, topic);
                if (question == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy câu hỏi!"
                    });
                }

                return Ok(new
                {
                    message = "Lấy câu hỏi ngẫu nhiên theo chủ đề thành công!",
                    info = question
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("{username}/get-all-questions/{topic}")]
        public async Task<IActionResult> GetAllQuestionsByTopic([FromRoute] string username, [FromRoute] string topic)
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
                var questions = await _questionService.GetAllQuestionByTopic(username, topic);
                if (questions == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy câu hỏi!"
                    });
                }

                return Ok(new
                {
                    message = "Lấy danh sách câu hỏi theo chủ đề thành công!",
                    info = questions
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{username}/delete-question")]
        public async Task<IActionResult> DeleteQuestion([FromBody] QuestionDTO question, [FromRoute] string username)
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
                var deletedQuestion = await _questionService.DeleteQuestion(question, username);
                if (!deletedQuestion)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy câu hỏi!"
                    });
                }

                return Ok(new
                {
                    message = "Xóa câu hỏi thành công!",
                    info = deletedQuestion
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Đã xảy ra lỗi!",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPatch("{username}/update-question")]
        public async Task<IActionResult> UpdateQuestion([FromBody] QuestionUpdateRequest request, [FromRoute] string username)
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
                var updatedQuestion = await _questionService.UpdateQuestion(request.OldQuestion, request.NewQuestion, username);
                if (updatedQuestion == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy câu hỏi!"
                    });
                }

                return Ok(new
                {
                    message = "Cập nhật câu hỏi thành công!",
                    info = updatedQuestion
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Đã xảy ra lỗi!",
                    error = ex.Message
                });
            }
        }

        public class QuestionUpdateRequest
        {
            public QuestionDTO OldQuestion { get; set; } 
            public QuestionDTO NewQuestion { get; set; } 
        }

    }
}
