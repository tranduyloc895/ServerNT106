using API_Server.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API_Server.Services
{
    public class SingleChatService
    {
        private readonly IMongoCollection<SingleChat> _messages;

        public SingleChatService(MongoDbService db)
        {
            _messages = db.Messages;
        }

        // Send a message
        public async Task SendMessageAsync(SingleChat message)
        {
            
            await _messages.InsertOneAsync(message);
        }
        
        // Get messages between two users
        public async Task<List<SingleChat>> GetMessagesAsync(string user1, string user2)
        {
            var filter = Builders<SingleChat>.Filter.Or(
                Builders<SingleChat>.Filter.And(
                    Builders<SingleChat>.Filter.Eq(m => m.Sender, user1),
                    Builders<SingleChat>.Filter.Eq(m => m.Receiver, user2)
                ),
                Builders<SingleChat>.Filter.And(
                    Builders<SingleChat>.Filter.Eq(m => m.Sender, user2),
                    Builders<SingleChat>.Filter.Eq(m => m.Receiver, user1)
                )
            );

            return await _messages.Find(filter).SortBy(m => m.Timestamp).ToListAsync();
        }
    }
}