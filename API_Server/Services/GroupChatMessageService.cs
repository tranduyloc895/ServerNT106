using API_Server.Models;
using MongoDB.Driver;

namespace API_Server.Services
{
    public class GroupChatMessageService
    {
        private readonly IMongoCollection<GroupChatMessage> groupChatMessage;
        private readonly AesService encryptionService;
        //private readonly string privateKey;
        public GroupChatMessageService(MongoDbService db, AesService aesService)
        {
            groupChatMessage = db.GroupChatMessage;
            encryptionService = aesService;
            
        }

        public async Task SendMessage(GroupChatMessage message)
        {
            
            await groupChatMessage.InsertOneAsync(message);
        }
        public async Task SendAnnouncement(string groupId, string content, string key)
        {
            var msgContent = encryptionService.Encrypt(content, key);
            var msg = new GroupChatMessage
            {
                Sender = "Hệ thống",
                GroupId = groupId,
                Content = msgContent,
                TimeStamp = DateTime.UtcNow,

            };
            await groupChatMessage.InsertOneAsync(msg);
        }
        public async Task<List<GroupChatMessage>> GetMessageByGroupId(string groupId, string username)
        {
            var filter = Builders<GroupChatMessage>.Filter.Eq(gr => gr.GroupId, groupId);
            var msgs = await groupChatMessage.Find(filter).SortBy(msg => msg.TimeStamp).ToListAsync();
            
            return msgs;
        }
        public async Task<bool> DeleteAllMessageByGroupId(string groupId)
        {
            try
            {
                var filter = Builders<GroupChatMessage>.Filter.Eq(msg => msg.GroupId, groupId);
                var result = await groupChatMessage.DeleteManyAsync(filter);

                return result.DeletedCount > 0; 
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa tin nhắn: {ex.Message}");
            }
        }
    }
}
