using API_Server.DTOs;
using API_Server.Models;
using API_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Security.Claims;

namespace API_Server.Controllers
{
    [ApiController]
    [Route("/api/groups")]
    public class GroupController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly GroupService _chatGroupService;
        
        private readonly MongoDbService _context;
        
        private readonly JwtService _jwtService;
        public GroupController(UserService userService, GroupService cgs, MongoDbService context, JwtService jwtService)
        {
            _userService = userService;
            _chatGroupService = cgs;
            _context = context;
            _jwtService = jwtService;
        }

        //POST METHOD
        [Authorize]
        [HttpPost("{username}/create")]
        public async Task<ActionResult<Group>> CreateGroup(string username,[FromBody] CreateGroup groupModel)
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
                var user = await _userService.GetUserByUserName(username);
                var creator = new MemberRole
                {
                    Name = user.Name,
                    Username = username,
                    Role = "003"
                };
                groupModel.Members.Add(creator);
                var group = new Group
                {
                    Name = groupModel.Name,
                    Description = groupModel.Description,
                    Creator = username,
                    Members = groupModel.Members,   
                };

                var createdGroup = await _chatGroupService.CreateGroup(group);

                return Ok(new
                {
                    message = "Create group sucessfully!",
                    info = createdGroup
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("{groupId}/add-user")]
        public async Task<IActionResult> AddUserToGroup(string groupId, [FromBody] AddUserRequestDTO userReq)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var username = await _jwtService.GetUsernameFromToken(authorizationHeader);
            if(username == null)
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }    
            try
            {
                var checkJoined = await _chatGroupService.IsInGroup(username, groupId);
                if (!checkJoined)
                {
                    return BadRequest(new
                    {
                        message = "Bạn chưa tham gia vào nhóm này!"
                    });
                }
                    
                var user = await _userService.GetUserByUserName(userReq.Username);
                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy người dùng!"
                    });
                }
                var checkFriend = await _userService.IsFriend(username, userReq.Username);
                if (!checkFriend)
                {
                    return BadRequest(new
                    {
                        message = "Hai bạn chưa là bạn bè nên không thể thêm vào nhóm!"
                    });
                }
                var group = await _chatGroupService.GetGroupById(groupId);
                if (group == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy nhóm"
                    });
                }
                var callerRole = group.Members.FirstOrDefault(m => m.Username == username)?.Role;
                if(callerRole == "002")
                {
                    return StatusCode(403, new
                    {
                        message = "Chỉ Admin có thể thêm thành viên!"
                    });
                }    

                var check = await _chatGroupService.AddUserToGroup(groupId, userReq.Username, user.Name, userReq.Role);
                

                if(!check)
                {
                    return BadRequest(new
                    {
                        message = "Không thể thêm ng dùng"
                    });
                }
                return Ok(new
                {
                    message = "Thêm người dùng thành công!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("join-request/{groupId}")]
        public async Task<IActionResult> JoinGroup(string groupId)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var username = await _jwtService.GetUsernameFromToken(authorizationHeader);
            if (username == null)
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }
            try
            {
                await _chatGroupService.JoinGroupReq(groupId, username);
                return Ok(new
                {
                    message = "Đã gửi yêu cầu tới admin",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("{groupId}/add-user-request")]
        public async Task<IActionResult> AddUserToGroupRequest(string groupId, [FromBody] AddUserRequestDTO userReq)
        {
            
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var username = await _jwtService.GetUsernameFromToken(authorizationHeader);
            if (username == null)
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }

            try
            {
                var checkJoined = await _chatGroupService.IsInGroup(username, groupId);
                if (!checkJoined)
                {
                    return BadRequest(new
                    {
                        message = "Bạn chưa tham gia vào nhóm này!"
                    });
                }
                var user = await _userService.GetUserByUserName(userReq.Username);
                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy người dùng!"
                    });
                }
                var checkFriend = await _userService.IsFriend(username, userReq.Username);
                if (!checkFriend)
                {
                    return BadRequest(new
                    {
                        message = "Hai bạn chưa là bạn bè nên không thể thêm vào nhóm!"
                    });
                }
                var check = await _chatGroupService.AddUserToGroupRequest(groupId, userReq.Username);
                if(!check)
                {
                    return BadRequest(new
                    {
                        message = "Người dùng đã là thành viên!"
                    });
                }    
                return Ok(new
                {
                    message = "Đã gửi request tới admin!"
                });

            }
            catch (Exception ex) 
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("{groupId}/accept-join-req/{reqUsername}")]
        public async Task<IActionResult> AcptJoinReq(string groupId,string reqUsername)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var username = await _jwtService.GetUsernameFromToken(authorizationHeader);
            if (username == null)
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }
            try
            {
                var checkJoined = await _chatGroupService.IsInGroup(username, groupId);
                if (!checkJoined)
                {
                    return BadRequest(new
                    {
                        message = "Bạn chưa tham gia vào nhóm này!"
                    });
                }

                var member = await _chatGroupService.GetMemberInGroup(groupId, username);
                if (member.Role == "002")
                {
                    return BadRequest(new
                    {
                        message = "Chỉ có admin mới được xóa người dùng!"
                    });
                }

                var acpt = await _chatGroupService.AcptJoinReq(groupId, reqUsername);
                if (acpt)
                {
                    return Ok(new
                    {
                        message = "Đã thêm thành viên!"
                    });
                }    
                else
                {
                    return BadRequest(new
                    {
                        message = "Không thể chấp nhận người dùng này!"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("{groupId}/remove-join-req/{reqUsername}")]
        public async Task<IActionResult> DelJoinReq(string groupId, string reqUsername)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var username = await _jwtService.GetUsernameFromToken(authorizationHeader);
            if (username == null)
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }
            try
            {
                var checkJoined = await _chatGroupService.IsInGroup(username, groupId);
                if (!checkJoined)
                {
                    return BadRequest(new
                    {
                        message = "Bạn chưa tham gia vào nhóm này!"
                    });
                }

                var member = await _chatGroupService.GetMemberInGroup(groupId, username);
                if (member.Role == "002")
                {
                    return BadRequest(new
                    {
                        message = "Chỉ có admin mới được xóa người dùng!"
                    });
                }

                var acpt = await _chatGroupService.DelJoinReq(groupId, reqUsername);
                if (acpt)
                {
                    return Ok(new
                    {
                        message = "Đã xóa yêu cầu vào nhóm!"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        message = "Không thể chấp nhận người dùng này!"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("{groupId}/remove-member/{memUsername}")]
        public async Task<IActionResult> RemoveMemberFromGroup(string groupId, string memUsername)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var username = await _jwtService.GetUsernameFromToken(authorizationHeader);
            if (username == null)
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }
            try
            {
                var checkJoined = await _chatGroupService.IsInGroup(username, groupId);
                if (!checkJoined)
                {
                    return BadRequest(new
                    {
                        message = "Bạn chưa tham gia vào nhóm này!"
                    });
                }

                var member = await _chatGroupService.GetMemberInGroup(groupId, username);
                if (member.Role == "002")
                {
                    return BadRequest(new
                    {
                        message = "Chỉ có admin mới được xóa người dùng!"
                    });
                }

                var removeUser = await _chatGroupService.RemoveUserFromGroup(groupId, memUsername);
                if (removeUser)
                {
                    return Ok(new
                    {
                        message = "Đã xóa người dùng khỏi nhóm!"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        message = "Không thể xóa người dùng này!"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("leave-group/{groupId}")]
        public async Task<IActionResult> LeaveGroup(string groupId)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var username = await _jwtService.GetUsernameFromToken(authorizationHeader);
            if (username == null)
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }
            try
            {
                var checkJoined = await _chatGroupService.IsInGroup(username, groupId);
                if (!checkJoined)
                {
                    return BadRequest(new
                    {
                        message = "Bạn chưa tham gia vào nhóm này!"
                    });
                }
                var group = await GetGroupByGroupId(groupId);
                if (group == null)
                {
                    return NotFound(new { message = "Nhóm không tồn tại!" });
                }
                await _chatGroupService.LeaveGroup(groupId, username);
                return Ok(new
                {
                    message = "Đã rời khỏi nhóm!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal Server Error",
                    detail = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("{groupId}/change-role/{reqUsername}")]
        public async Task<IActionResult> ChangeRole(string groupId, string reqUsername)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var username = await _jwtService.GetUsernameFromToken(authorizationHeader);
            if (username == null)
            {
                return Unauthorized(new
                {
                    message = "Người dùng không tồn tại"
                });
            }
            try
            {
                var group = await _chatGroupService.GetGroupById(groupId);
                if (group == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy nhóm"
                    });
                }
                var checkJoined = await _chatGroupService.IsInGroup(username, groupId);
                if (!checkJoined)
                {
                    return BadRequest(new
                    {
                        message = "Bạn chưa tham gia nhóm này!",
                    });
                }
                var member = await _chatGroupService.GetMemberInGroup(groupId, username);
                if (member.Role == "002")
                {
                    return BadRequest(new
                    {
                        message = "Bạn không được chỉnh sửa vai trò!",
                    });
                }
                var check = await _chatGroupService.ChangeRoleUser(groupId, reqUsername);
                if (check)
                {
                    return Ok(new
                    {
                        message = "Đã cập nhật quyền của người dùng!",
                    });
                }
                return BadRequest(new
                {
                    message = "Không thể cập nhật quyền của người dùng!",
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

        

        //GET METHOD
        [Authorize]
        [HttpGet("get-group/{groupId}")]
        public async Task<IActionResult> GetGroupByGroupId(string groupId)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var username = await _jwtService.GetUsernameFromToken(authorizationHeader);
            if (username == null)
            {
                return Unauthorized(new
                {
                    message = "Người dùng không tồn tại"
                });
            }    
            var group = await _chatGroupService.GetGroupById(groupId);
            if (group == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy nhóm"
                });
            }

            var checkJoined = await _chatGroupService.IsInGroup(username, groupId);
            if (!checkJoined)
            {
                return BadRequest(new
                {
                    message = "Bạn chưa tham gia nhóm này!",
                    name = group.Name,
                    description = group.Description
                });
            }


            return Ok(new
            {
                message = "Đã lấy nhóm thành công!",
                id = group.Id.ToString(),
                name = group.Name,
                description = group.Description,
                members = group.Members,
                keys = group.SessionKeyEncrypted
            });
        }

        [Authorize]
        [HttpGet("search")]
        public async Task<IActionResult> SearchGroup([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if(!_jwtService.IsValidate(authHeader))
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }
            try
            {
                var (groups, totalPages) = await _chatGroupService.SearchGroup(query, page, pageSize);
                if (groups == null || totalPages == 0)
                {
                    return NotFound(new
                    {
                        total = 0,
                        message = "Không tìm thấy nhóm!"
                    });
                }
                return Ok(new
                {
                    total = groups.Count,
                    totalPages,
                    currentPage =page,
                    data = groups
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

        [Authorize]
        [HttpGet("get-join-list/{groupId}")]
        public async Task<IActionResult> GetJoinList(string groupId)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var username = await _jwtService.GetUsernameFromToken(authorizationHeader);
            if (username == null)
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }
            try
            {
                var checkJoined = await _chatGroupService.IsInGroup(username, groupId);
                if (!checkJoined)
                {
                    return BadRequest(new
                    {
                        message = "Bạn chưa tham gia vào nhóm này!"
                    });
                }
                var joinReq = await _chatGroupService.GetJoinList(groupId);
                if(joinReq == null)
                {
                    return BadRequest(new
                    {
                        message = "Không tìm thấy nhóm"
                    });
                }
                return Ok(new
                {
                    message = "Lấy danh sách yêu cầu thành công!",
                    total = joinReq.Count,
                    data = joinReq
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

        [Authorize]
        [HttpGet("get-member-list/{groupId}")]
        public async Task<IActionResult> GetMemList(string groupId)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var username = await _jwtService.GetUsernameFromToken(authHeader);
            if (username == null)
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }
            try
            {
                var checkJoined = await _chatGroupService.IsInGroup(username, groupId);
                if (!checkJoined)
                {
                    return BadRequest(new
                    {
                        message = "Bạn chưa tham gia vào nhóm này!"
                    });
                }
                var (memList, total) = await _chatGroupService.GetMemList(groupId);
                if(memList == null || total == 0)
                {
                    return BadRequest(new
                    {
                        message = "Không có thành viên"
                    });
                }
                return Ok(new
                {
                    message = "Đã lấy được danh sách",
                    total = total,
                    data = memList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        //DELETE METHOD
        [Authorize]
        [HttpDelete("delete-group/{groupId}")]
        public async Task<IActionResult> DeleteGroup(string groupId)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var username = await _jwtService.GetUsernameFromToken(authHeader);

            if (username == null)
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }

            try
            {
                var group = await _chatGroupService.GetGroupById(groupId);
                if(group == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy nhóm!"
                    });
                }    

                if(group.Creator != username)
                {
                    return StatusCode(403, new
                    {
                        message = "Bạn không được xóa group!",
                    });
                }

                var isDeleted = await _chatGroupService.DeleteGroup(groupId, username);
                if(isDeleted)
                {
                    return Ok(new
                    {
                        message = "Đã xóa nhóm!"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        message = "Xóa group thất bại!"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPatch("update-group-info/{groupId}")]
        public async Task<IActionResult> UpdateGroupInfo(string groupId, [FromBody] UpdateGroupDTO updateGroup)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var username = await _jwtService.GetUsernameFromToken(authHeader);

            if (username == null)
            {
                return Unauthorized(new
                {
                    message = "Yêu cầu không hợp lệ!"
                });
            }
            try
            {
                var group = await _chatGroupService.GetGroupById(groupId);
                if(group == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy nhóm!",

                    });
                }

                if(updateGroup == null)
                {
                    return BadRequest(new
                    {
                       mesage = "Không có cập nhật!",
                    });
                }    
                
                var check = await _chatGroupService.UpdateGroupInfo(groupId, updateGroup.Name, updateGroup.Description);
                if(check)
                {
                    return Ok(new
                    {
                        message = "Đã cập nhật thông tin nhóm!",
                    });
                }    
               
                return BadRequest(new
                {
                    message = "Không cập nhật được thông tin!"
                });
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }
}
