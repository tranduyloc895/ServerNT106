using Microsoft.AspNetCore.Mvc;
using API_Server.DTOs;
using API_Server.Models;
using API_Server.Services;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;

namespace API_Server.Controllers
{

    [Route("api/user-task")]
    [ApiController]
    public class UserTaskController : Controller
    {
        private readonly TaskService _taskService;
        private readonly JwtService _jwtService;

        public UserTaskController(TaskService taskService, JwtService jwtService)
        {
            _taskService = taskService;
            _jwtService = jwtService;
        }

        [Authorize]
        [HttpPost("{username}/create-task")]
        public async Task<IActionResult> CreateTaskAsync([FromRoute] string username, [FromBody] UserTask task)
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
                var taskDto = await _taskService.CreateTaskAsync(task, username);
                if (taskDto == null)
                {
                    return BadRequest(new
                    {
                        message = "Tạo task không thành công!"
                    });
                }
                return Ok(new
                {
                    message = "Tạo task thành công!",
                    task = taskDto
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("{username}/get-all-task")]
        public async Task<IActionResult> GetAllTaskAsync([FromRoute] string username)
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
                var tasks = await _taskService.GetTasksAsync(username);
                if (tasks == null)
                {
                    return BadRequest(new
                    {
                        message = "Không tìm thấy task!"
                    });
                }
                return Ok(new
                {
                    message = "Lấy task thành công!",
                    tasks = tasks.Select(task => new
                    {
                        task.Description,
                        task.Category,
                        task.IsCompleted,
                        StartDate = task.StartDateFormatted, 
                        EndDate = task.EndDateFormatted     
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("{username}/get-task-category/{category}")]
        public async Task<IActionResult> GetTaskByCategoryAsync([FromRoute] string username, [FromRoute] string category)
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
                var tasks = await _taskService.GetTaskByCategoryAsync(username, category);
                if (tasks == null)
                {
                    return BadRequest(new
                    {
                        message = "Không tìm thấy task!"
                    });
                }
                return Ok(new
                {
                    message = $"Lấy task {category} thành công!",
                    tasks = tasks.Select(task => new
                    {
                        task.Description,
                        task.Category,
                        task.IsCompleted,
                        StartDate = task.StartDateFormatted,
                        EndDate = task.EndDateFormatted
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("{username}/get-task-iscompleted")]
        public async Task<IActionResult> GetTaskIsCompletedAsync([FromRoute] string username)
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
                var tasks = await _taskService.GetTaskByIsComplete(username);
                if (tasks == null)
                {
                    return BadRequest(new
                    {
                        message = "Không tìm thấy task đã hoàn thành!"
                    });
                }
                return Ok(new
                {
                    tasks = tasks.Select(task => new
                    {
                        task.Description,
                        task.Category,
                        task.IsCompleted,
                        StartDate = task.StartDateFormatted,
                        EndDate = task.EndDateFormatted
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("{username}/get-task-ispending")]
        public async Task<IActionResult> GetTaskIsPendingAsync([FromRoute] string username)
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
                var tasks = await _taskService.GetTaskIsPending(username);
                if (tasks == null)
                {
                    return BadRequest(new
                    {
                        message = "Không tìm thấy task!"
                    });
                }
                return Ok(new
                {
                    message = "Lấy task đang thực hiện thành công!",
                    tasks = tasks.Select(task => new
                    {
                        task.Description,
                        task.Category,
                        task.IsCompleted,
                        StartDate = task.StartDateFormatted,
                        EndDate = task.EndDateFormatted
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpDelete("{username}/delete-task")]
        public async Task<IActionResult> DeleteTaskAsync([FromRoute] string username, [FromBody] TaskDTO task)
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
                var result = await _taskService.DeleteTask(username, task);
                if (!result)
                {
                    return BadRequest(new
                    {
                        message = "Xóa task không thành công!"
                    });
                }
                return Ok(new
                {
                    message = "Xóa task thành công!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("{username}/get-task-day")]
        public async Task<IActionResult> GetTaskByDayAsync([FromRoute] string username, [FromQuery] string date)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !_jwtService.IsValidate(authorizationHeader))
            {
                return Unauthorized(new { message = "Yêu cầu không hợp lệ!" });
            }

            try
            {
                DateTime parsedDate;
                if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                {
                    return BadRequest(new { message = "Ngày không hợp lệ!" });
                }

                var tasks = await _taskService.GetTaskByDate(username, parsedDate);
                if (tasks == null || tasks.Count == 0)
                {
                    return NotFound(new { 
                        message = "Không tìm thấy task nào!" 
                    });
                }

                return Ok(new
                {
                    message = "Lấy task thành công!",
                    tasks = tasks.Select(task => new
                    {
                        task.Description,
                        task.Category,
                        task.IsCompleted,
                        StartDate = task.StartDate.ToString("yyyy-MM-dd"),
                        EndDate = task.EndDate.ToString("yyyy-MM-dd")
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [Authorize]
        [HttpPatch("{username}/update-task")]
        public async Task<IActionResult> UpdateTaskAsync([FromRoute] string username, [FromBody] TaskUpdateRequest request)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (!_jwtService.IsValidate(authorizationHeader))
            {
                return Unauthorized(new { message = "Yêu cầu không hợp lệ!" });
            }

            try
            {

                var result = await _taskService.UpdateTask(username, request.OldTask, request.NewTask);
                if (!result)
                {
                    return BadRequest(new { message = "Cập nhật task không thành công!" });
                }

                return Ok(new
                {
                    message = "Cập nhật task thành công!",
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("{username}/get-task-description/{description}")]
        public async Task<IActionResult> GetTaskByDescriptionAsync([FromRoute] string username, [FromRoute] string description)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (!_jwtService.IsValidate(authorizationHeader))
            {
                return Unauthorized(new { message = "Yêu cầu không hợp lệ!" });
            }

            try
            {
                var tasks = await _taskService.GetTaskByDescription(username, description);
                if (tasks == null)
                {
                    return NotFound(new { message = "Không tìm thấy task!" });
                }

                return Ok(new
                {
                    tasks = tasks
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        public class TaskUpdateRequest()
        { 
            public TaskDTO OldTask { get; set; }
            public TaskDTO NewTask { get; set; }
        }


    }
}
