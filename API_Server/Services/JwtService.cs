using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using API_Server.Models;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using NetStudy.Services;

namespace API_Server.Services
{
    public class JwtService
    {
        private readonly IConfiguration _configuration;
       
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly IMongoCollection<TokenData> _tokenData;
        private readonly UserService _userService;
        private readonly RsaService _rsaService;

        public JwtService(IConfiguration configuration, MongoDbService db, UserService userService, RsaService rsaService)
        {
            _configuration = configuration;
            _tokenData = db.Tokens;
            _userService = userService;
            _rsaService = rsaService;
            _secret = _configuration["JwtSettings:Secret"] ?? throw new ArgumentNullException(nameof(_secret));
            _issuer = _configuration["JwtSettings:Issuer"] ?? throw new ArgumentNullException(nameof(_issuer));
            _audience = _configuration["JwtSettings:Audience"] ?? throw new ArgumentNullException(nameof(_audience));
        }

        public string EncryptAes(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = GetKeyBytes(_secret);
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
        public string DecryptAES(string encryptedText)
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
                aes.Key = GetKeyBytes(_secret);
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

        public string GenerateAccessToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            
            var jti = Guid.NewGuid().ToString();

            var claims = new[]
            {

                new Claim(JwtRegisteredClaimNames.Name, user.Name),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                new Claim("userName", user.Username),
                new Claim("userId", user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(10),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secret);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }
        public bool IsValidate(string authHeader)
        {
            try
            {
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return false;
                }

                var accessToken = authHeader.Substring("Bearer ".Length).Trim();

                var claimsPrincipal = ValidateToken(accessToken);
                if (claimsPrincipal == null)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<string> GetUsernameFromToken(string authHeader)
        {
            var accessToken = authHeader.Substring("Bearer ".Length).Trim();
            var claimsPrincipal = ValidateToken(accessToken);//Trả về giá trị người dùng của token
            if (claimsPrincipal == null)
            {
                return null;
            }

            var usernameClaim = claimsPrincipal.FindFirst("userName");//Tìm username của token
            if (usernameClaim == null || string.IsNullOrEmpty(usernameClaim.Value))
            {
                return null;
            }
            return usernameClaim.Value;
        }
        public async Task<TokenData> GetRefreshToken(string token)
        {
            var refreshToken = await _tokenData.Find(rt => rt.RefreshToken == token).FirstOrDefaultAsync();
            return refreshToken;

        }
        public async Task<bool> ValidateRefreshToken(string token, string userName)
        {
            var tokenData = await GetRefreshToken(token);

            if(tokenData == null || tokenData.Username != userName)
            {
                return false;
            }
            if (tokenData.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return false;
            }
            if(tokenData.RefreshTokensUsed.Contains(token))
            {
                return false ;
            }    
       
            return true;
        }
        public async Task SaveToken(TokenData token)
        {
            var existedToken = await _tokenData.Find(td => td.Username == token.Username).FirstOrDefaultAsync();

            if (existedToken != null)
            {
                existedToken.RefreshToken = token.RefreshToken;
                existedToken.RefreshTokensUsed = token.RefreshTokensUsed;
                existedToken.RefreshTokenExpiryTime = token.RefreshTokenExpiryTime;
                existedToken.Jti = token.Jti;
                existedToken.PublicKey = token.PublicKey ?? existedToken.PublicKey;
                existedToken.PrivateKey = token.PrivateKey ?? existedToken.PrivateKey;

                await _tokenData.ReplaceOneAsync(td => td.Id == existedToken.Id, existedToken);
            }
            else
            {
                
               
                await _tokenData.InsertOneAsync(token);
            }
        }
        
        public async Task<(string, string)> GenerateNewAccessToken(string refreshToken, string userName)
        {
            var token = await GetRefreshToken(refreshToken);
            var isValidated = await ValidateRefreshToken(refreshToken, userName);
            if (!isValidated)
            {
                throw new UnauthorizedAccessException("Invalid token!");
            }

            token.RefreshTokensUsed.Add(refreshToken);

            var user = await _userService.GetUserByUserName(userName);
            var newAccessToken = GenerateAccessToken(user);

            var newRefreshToken = GenerateRefreshToken();
            token.RefreshToken = newRefreshToken;
            token.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await SaveToken(token);

            return (newAccessToken, newRefreshToken);
        }
        public string GetJtiFromAccessToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            var jti = jwtToken?.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Jti)?.Value;
            return jti ?? string.Empty;
        }
        public async Task<string> EncryptMessage(string tokenId, string message)
        {
            var tokenData = await _tokenData.Find(t => t.Id == tokenId).FirstOrDefaultAsync();
            if (tokenData != null || string.IsNullOrEmpty(tokenData.PublicKey))
            {
                throw new Exception("Key not found!");

            }
            var keyBytes = Convert.FromBase64String(tokenData.PublicKey);

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportRSAPublicKey(keyBytes, out _);
                var messageBytes = Encoding.UTF8.GetBytes(message);
                var encryptedBytes = rsa.Encrypt(messageBytes, false);

                return Convert.ToBase64String(encryptedBytes);
            }
        }
    }
}
