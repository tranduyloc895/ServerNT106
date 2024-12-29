using API_Server.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using NetStudy.Services;
using System.Data;
using UglyToad.PdfPig.Core;


namespace API_Server.Services
{
    public class GroupService
    {
        private readonly IMongoCollection<Group> _chatGroups;
        private readonly GroupChatMessageService _groupChatMessageService;
        private readonly UserService _userService;
        private readonly IMongoCollection<GroupChatMessage> _groupChatMessage;
        private readonly AesService _aesService;
        private readonly RsaService _rsaService;
        private readonly JwtService _jwtService;
        private string _key;
        public GroupService(MongoDbService db, UserService userService,GroupChatMessageService groupChatMessageService, AesService aesService, RsaService rsaService, JwtService jwtService)
        {
            _chatGroups = db.ChatGroup;
            _userService = userService;
            _groupChatMessage = db.GroupChatMessage;
            _aesService = aesService;
            _rsaService = rsaService;
            _jwtService = jwtService;
            _groupChatMessageService = groupChatMessageService;
        }

        public async Task<Group> CreateGroup(Group chatGroup)
        {

            string sessionKey = _aesService.GenerateAesKey();
            var user = await _userService.GetUserByUserName(chatGroup.Creator);
            chatGroup.GroupKey = _jwtService.EncryptAes(sessionKey);
            chatGroup.SessionKeyEncrypted[chatGroup.Creator] = _rsaService.Encrypt(sessionKey, user.PublicKey);
            await _chatGroups.InsertOneAsync(chatGroup);
            return chatGroup;
        }
        //public async Task<string> GetKey(string groupId, string username)
        //{
        //    var filter = Builders<KeyModel>.Filter.And(
        //            Builders<KeyModel>.Filter.Eq(g => g.GroupId, groupId),
        //            Builders<KeyModel>.Filter.Eq(g => g.Username, username)
        //    );
        //    var checkKey = await _keys.Find(filter).FirstOrDefaultAsync();
        //    if (checkKey != null)
        //    {
        //        return checkKey.Key;
        //    }
        //    else
        //    {
        //        var key = await SaveKeyByGroupId(groupId, username);
        //        return key;
        //    } 

        //}
        //public async Task<string> SaveKeyByGroupId(string id, string username)
        //{

        //    var key = await _aesService.GenerateAesKey(username);
        //    return key;
        //}    
        public void AssignPrivateKeyToGroup(string privateKey)
        {
            _key = privateKey;
        }
        public async Task<Group> GetGroupById(string groupId)
        {
            

            return await _chatGroups.Find(g => g.Id.ToString() == groupId).FirstOrDefaultAsync(); 
        }

        public async Task<Group> GetGroupByName(string groupName)
        {
            return await _chatGroups.Find(g => g.Name == groupName).FirstOrDefaultAsync();
        }
        public async Task<MemberRole> GetMemberInGroup(string groupId, string username)
        {
            var group = await GetGroupById(groupId);
            var check = await IsInGroup(username, groupId);
            if (!check)
            {
                return null;
            }
            try
            {
                var member = group.Members.FirstOrDefault(m => m.Username == username);
                return member;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task UpdateGroup(Group group)
        {
            await _chatGroups.ReplaceOneAsync(gr => gr.Id.ToString() == group.Id.ToString(), group);
        }

        public async Task<bool> IsInGroup(string username, string groupId)
        {
            var group = await GetGroupById(groupId);
            if (group == null)
            {
                return false;
            }
            return group.Members.Any(m => m.Username == username);
        }
        // thêm user vào group cho admin
        public async Task<bool> AddUserToGroup(string groupId, string userName, string name ,string role)
        {
            var group = await GetGroupById(groupId);
            if (group == null) 
                throw new Exception("Group not found.");
            var isJoined = await IsInGroup(userName, groupId);
            if (isJoined)
            {
                Console.WriteLine("Da join");
                return false;
            }
            if(role != "User" && role != "Admin")
            {
                Console.WriteLine("Sai role");
                return false;
            }    
            var member = new MemberRole
            {
                Name = name,
                Username = userName,
                Role = (role == "Admin") ? "001" : "002"
            };

            var groupKey = _jwtService.DecryptAES(group.GroupKey);


            var newMem = await _userService.GetUserByUserName(userName);
            group.SessionKeyEncrypted[userName] = _rsaService.Encrypt(groupKey, newMem.PublicKey);
            await _groupChatMessageService.SendAnnouncement(groupId, $"Thêm người dùng {userName} là {role}", groupKey);
            group.Members.Add(member);
            var addGroup = await _userService.AddGroupToUser(userName, groupId);
            if (!addGroup)
            {
                Console.WriteLine("Khong add dc!");
                return false;
            }
            
            await UpdateGroup(group);
            return true;
        }
        public async Task<bool> AddUserToGroupRequest(string groupId, string reqUsername)
        {
            var isJoined = await IsInGroup(reqUsername, groupId);
            if (isJoined)
            {
                return false;
            }
            
            var update = Builders<Group>.Update.AddToSet(g => g.MemberRequest, reqUsername);
            await _chatGroups.UpdateOneAsync(g => g.Id.ToString() == groupId, update);
            return true;
        }
        public async Task JoinGroupReq(string groupId, string reqUsername)
        {
            var update = Builders<Group>.Update.AddToSet(g => g.MemberRequest, reqUsername);
            await _chatGroups.UpdateOneAsync(g => g.Id.ToString() == groupId, update);
        }
        public async Task<List<string>> GetJoinList(string groupId)
        {
            
            try
            {
                var group = await GetGroupById(groupId);
                if (group == null)
                {
                    return null;
                }
                var joinReq = group.MemberRequest;
                if(joinReq.Count == 0)
                {
                    return new List<string>();
                }  
                return joinReq;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<(List<MemberRole>,int)> GetMemList(string groupId)
        {
            try
            {
                var group = await GetGroupById(groupId);
                if (group == null)
                {
                    return (null,0);
                }
                var memList = group.Members;
                if(memList.Count == 0)
                {
                    return (new List<MemberRole>(), 0);
                }
                int total = memList.Count;
                return (memList, total);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<bool> AcptJoinReq(string groupId, string req)
        {
            try
            {
                var group = await GetGroupById(groupId);
                if (group == null)
                {
                    return false;
                }
                var user = await _userService.GetUserByUserName(req);
                if (user == null)
                {
                    return false;
                }
                
                var member = new MemberRole
                {
                    Name = user.Name,
                    Username = user.Username,
                    Role = "002"
                };
                var groupKey = _jwtService.DecryptAES(group.GroupKey);


                var newMem = await _userService.GetUserByUserName(req);
                group.SessionKeyEncrypted[req] = _rsaService.Encrypt(groupKey, newMem.PublicKey);

                Console.WriteLine("debug 3");
                await _groupChatMessageService.SendAnnouncement(groupId, $"Thêm người dùng {req} là User", groupKey);

                group.MemberRequest.Remove(req);
                group.Members.Add(member);
                var addGroup = await _userService.AddGroupToUser(req, groupId);
                if(!addGroup)
                {
                    return false;
                }
                await UpdateGroup(group);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        public async Task<bool> DelJoinReq(string groupId, string username)
        {
            try
            {
                var group = await GetGroupById(groupId);
                if(group == null)
                {
                    return false ;
                }    
                var reqUser = await _userService.GetUserByUserName(username);
                if(reqUser == null)
                {
                    return false;
                }

                group.MemberRequest.Remove(username);
                await UpdateGroup(group);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }    
        public async Task LeaveGroup(string groupId, string username)
        {
            
            var group = await GetGroupById(groupId);
            if (group == null)
            {
                throw new Exception("Nhóm không tồn tại!");
            }

            var member = group.Members.FirstOrDefault(m => m.Username == username);
            if(member == null)
            {
                throw new Exception("Người dùng không phải thành viên của nhóm này!");
            }    
            if (!group.Members.Remove(member))
            {
                throw new Exception("Người dùng không phải thành viên của nhóm này!");
            }
            if (group.SessionKeyEncrypted.ContainsKey(username))
            {
                group.SessionKeyEncrypted.Remove(username);
            }
            var filter = Builders<Group>.Filter.Eq(g => g.Id, groupId);
            var update = Builders<Group>.Update
                .Set(g => g.Members, group.Members)
                .Set(g => g.SessionKeyEncrypted, group.SessionKeyEncrypted);

            await _chatGroups.UpdateOneAsync(filter, update);
            await _groupChatMessageService.SendAnnouncement(groupId, $"{username} đã rời khỏi nhóm", _jwtService.DecryptAES(group.GroupKey));
        }
        public async Task<bool> RemoveUserFromGroup(string groupId, string username)
        {
            var group = await GetGroupById(groupId);
            if(group == null)
            {
                return false;
            }    
            var user = await _userService.GetUserByUserName(username);
            if (user == null)
            {
                return false;
            }
            try
            {
                var member = group.Members.FirstOrDefault(m => m.Username == username);
                if(member == null)
                {
                    return false;
                }    
                group.Members.Remove(member);
                user.ChatGroup.Remove(groupId);
                await UpdateGroup(group);
                await _userService.UpdateUser(user);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }
        public async Task<(List<Group>, int)> SearchGroup(string query, int page, int pageSize)
        {
            if(page <= 0 || pageSize <= 0)
            {
                throw new ArgumentException("page và page size phải lớn hơn 0");
            }
            var filter = Builders<Group>.Filter.Or(
                    Builders<Group>.Filter.Regex("Id", new BsonRegularExpression(query, "i")),
                    Builders<Group>.Filter.Regex("Name", new BsonRegularExpression(query, "i"))
                );
            var total = await _chatGroups.CountDocumentsAsync(filter);

            var usersFound = await _chatGroups.Find(filter).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();

            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            return (usersFound, totalPages);
        }

        public async Task<bool> DeleteGroup(string groupId, string creatorName)
        {
   
            try
            {
                var (members, total) = await GetMemList(groupId);
                foreach (var member in members)
                {
                    var user = await _userService.GetUserByUserName(member.Username);
                    user.ChatGroup.Remove(groupId);
                    await _userService.UpdateUser(user);
                }
                var del = await _chatGroups.DeleteOneAsync(g => g.Id.ToString() == groupId);
                return del.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa group: {ex.Message}");
            }
        }

        public async Task<bool> UpdateGroupInfo(string groupId, string newName, string newDescription)
        {
            
            var filter = Builders<Group>.Filter.Eq(g => g.Id, groupId);
            var updates = new List<UpdateDefinition<Group>>();

            if(!string.IsNullOrEmpty(newName))
            {
                updates.Add(Builders<Group>.Update.Set(g => g.Name, newName));
            }    
            if(!string.IsNullOrEmpty(newDescription))
            {
                updates.Add(Builders<Group>.Update.Set(g => g.Description, newDescription));
            }    
            if(updates.Count == 0)
            {
                return false;
            }    

            var updateDef = Builders<Group>.Update.Combine(updates);
            var res = await _chatGroups.UpdateOneAsync(filter, updateDef);
            return res.ModifiedCount > 0;
        }

        public async Task<bool> ChangeRoleUser(string groupId, string reqUsername)
        {
            
            var member = await GetMemberInGroup(groupId, reqUsername);
            member.Role = (member.Role == "002") ? "001" : "002";
            var filter = Builders<Group>.Filter.Eq(g => g.Id, groupId);
            var group = await _chatGroups.Find(filter).FirstOrDefaultAsync();
            var existingMember = group.Members.FirstOrDefault(m => m.Username == reqUsername);
            if (existingMember != null)
            {
                existingMember.Role = member.Role;
            }
            var update = Builders<Group>.Update.Set(g => g.Members, group.Members);
            var result = await _chatGroups.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }
        
    }
}
