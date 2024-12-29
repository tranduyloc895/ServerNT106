using API_Server.Models;
using MongoDB.Driver;
using NetStudy.Services;
using System.Security.Cryptography;
using System.Text;

namespace API_Server.Services
{
    public class AesService
    {
        private readonly byte[] _key; 
        private readonly byte[] _iv; 
        private readonly IMongoCollection<KeyModel> _keys;
        private readonly UserService _userService;
        private readonly RsaService _rsaService;

        public AesService(MongoDbService db, UserService userService, RsaService rsaService)
        {
            _keys = db.KeyModel;
            _userService = userService;
            _rsaService = rsaService;
            
        }

        public AesService(byte[] key, byte[] iv)
        {
            _key = key;
            _iv = iv;
        }

        public string GenerateAesKey()
        {
            
            using var aes = Aes.Create();
            aes.GenerateKey();
            return Convert.ToBase64String(aes.Key);
 
        }

        public string Encrypt(string plainText, string key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = GetKeyBytes(key);
                aes.GenerateIV();

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    string encryptedText = Convert.ToBase64String(aes.IV) + ":" + Convert.ToBase64String(encryptedBytes);
                    return encryptedText;
                }
            }
        }
        public string DecryptAES(string encryptedText, string key)
        {
            string[] parts = encryptedText.Split(':');
            if (parts.Length != 2)
            {
                throw new FormatException("Dữ liệu mã hóa không hợp lệ.");
            }

            byte[] iv = Convert.FromBase64String(parts[0]);
            byte[] encryptedBytes = Convert.FromBase64String(parts[1]);

            using (Aes aes = Aes.Create())
            {
                aes.Key = GetKeyBytes(key);
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }
        private byte[] GetKeyBytes(string key)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
        }

        public byte[] GetKey() => _key;
        public byte[] GetIV() => _iv;
    }
}
