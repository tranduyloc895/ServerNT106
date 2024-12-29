using API_Server.Models;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace API_Server.Services
{
    public class DocumentService
    {
        private readonly IMongoCollection<Document> _documents;
        private readonly GridFSBucket _gridFS;
        private readonly UserService _userService;

        public DocumentService(MongoDbService db, UserService userService)
        {
            _documents = db.Documents;
            _gridFS = db.GridFS;
            _userService = userService;
        }

        // Upload a document
        public async Task<(string Id, string UploaderName)> UploadDocumentAsync(Document document, Stream fileStream)
        {
            var fileId = await _gridFS.UploadFromStreamAsync(document.Title, fileStream);
            document.Id = fileId.ToString();
            await _documents.InsertOneAsync(document);
            return (document.Id, document.UploaderName);
        }

        // Get a document detail
        public async Task<UploadResponse> GetDocumentByIdAsync(string id)
        {
            var document = await _documents.Find(d => d.Id == id).FirstOrDefaultAsync();
            if (document == null)
            {
                return null;
            }

            return new UploadResponse
            {
                DocumentId = document.Id,
                UploaderName = document.UploaderName,
                Title = document.Title,
                Description = document.Description,
                Tag = document.Tag,
                ShareWithFriends = document.ShareWithFriends,
                ShareWithGroups = document.ShareWithGroups,
                ShareWithAll = document.ShareWithAll
            };
        }

        // Download a document
        public async Task<Stream> DownloadDocumentAsync(string id)
        {
            var fileId = new MongoDB.Bson.ObjectId(id);
            var stream = await _gridFS.OpenDownloadStreamAsync(fileId);
            return stream;
        }

        // Delete a document
        public async Task DeleteDocumentAsync(string id)
        {
            var fileId = new MongoDB.Bson.ObjectId(id);
            await _gridFS.DeleteAsync(fileId);
            await _documents.DeleteOneAsync(d => d.Id == id);
        }

        // Check if a user has access to a document
        public async Task<bool> HasAccessAsync(string documentId, string username)
        {
            var document = await _documents.Find(d => d.Id == documentId).FirstOrDefaultAsync();
            if (document == null)
            {
                return false;
            }

            if (document.ShareWithAll)
            {
                return true;
            }

            if (document.ShareWithFriends && await _userService.IsFriend(document.UploaderName, username))
            {
                return true;
            }

            if (document.ShareWithGroups)
            {
                var userGroups = await _userService.GetUserGroups(username);
                foreach (var groupId in userGroups)
                {
                    if (await _userService.IsJoined(document.UploaderName, groupId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Search documents with access control
        public async Task<List<Document>> SearchDocumentsAsync(string keyword, string username)
        {
            var filter = Builders<Document>.Filter.Or(
                Builders<Document>.Filter.Regex("Title", new MongoDB.Bson.BsonRegularExpression(keyword, "i")),
                Builders<Document>.Filter.Regex("Description", new MongoDB.Bson.BsonRegularExpression(keyword, "i")),
                Builders<Document>.Filter.Regex("Tag", new MongoDB.Bson.BsonRegularExpression(keyword, "i"))
            );

            var documents = await _documents.Find(filter).ToListAsync();
            var accessibleDocuments = new List<Document>();

            foreach (var document in documents)
            {
                if (await HasAccessAsync(document.Id, username))
                {
                    accessibleDocuments.Add(document);
                }
            }

            return accessibleDocuments;
        }

        // Update a document
        public async Task UpdateDocumentAsync(string id, Document updatedDocument)
        {
            var filter = Builders<Document>.Filter.Eq(d => d.Id, id);
            var update = Builders<Document>.Update
                .Set(d => d.Title, updatedDocument.Title)
                .Set(d => d.Description, updatedDocument.Description)
                .Set(d => d.Tag, updatedDocument.Tag)
                .Set(d => d.ShareWithFriends, updatedDocument.ShareWithFriends)
                .Set(d => d.ShareWithGroups, updatedDocument.ShareWithGroups)
                .Set(d => d.ShareWithAll, updatedDocument.ShareWithAll);

            await _documents.UpdateOneAsync(filter, update);
        }
    }
}