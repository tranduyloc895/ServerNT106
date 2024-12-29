using API_Server.Models;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace API_Server.Services
{
    public class MongoDbService
    {
        private readonly IMongoDatabase _db;
        public GridFSBucket GridFS { get; }

        public MongoDbService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetConnectionString("DB_AWS"));
            _db = client.GetDatabase("Net_Study");
            GridFS = new GridFSBucket(_db);
        }

        public IMongoCollection<User> Users => _db.GetCollection<User>("User");
        public IMongoCollection<TokenData> Tokens => _db.GetCollection<TokenData>("TokenData");
        public IMongoCollection<Group> ChatGroup => _db.GetCollection<Group>("ChatGroup");
        public IMongoCollection<SingleChat> Messages => _db.GetCollection<SingleChat>("Messages");
        public IMongoCollection<GroupChatMessage> GroupChatMessage => _db.GetCollection<GroupChatMessage>("GroupChatMessage");
        public IMongoCollection<ListQuestion> ListQuestions => _db.GetCollection<ListQuestion>("Questions");
        public IMongoCollection<Otp> Otp => _db.GetCollection<Otp>("Otp");
        public IMongoCollection<KeyModel> KeyModel => _db.GetCollection<KeyModel>("Key");
        public IMongoCollection<ListTask> ListTasks => _db.GetCollection<ListTask>("Tasks");
        public IMongoCollection<Document> Documents => _db.GetCollection<Document>("Documents");
    }
}
