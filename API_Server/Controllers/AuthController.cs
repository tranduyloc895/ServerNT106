using Microsoft.AspNetCore.Mvc;
using API_Server.Services;
using API_Server.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;

namespace API_Server.Controllers
{
    [Route("api/refreshToken")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly MongoDbService _context;
        private readonly string _secret;
        private readonly IMongoCollection<TokenData> _tokenData;

        public AuthController(JwtService jwtService, MongoDbService context, IConfiguration configuration)
        {
            _jwtService = jwtService;
            _context = context;
            _secret = configuration["JwtSettings:Secret"] ?? throw new ArgumentNullException(nameof(_secret));
            _tokenData = _context.Tokens;
        }

        [HttpPost]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshToken request)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));

            try
            {
                // Validate the access token
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false
                };

                var principal = jwtTokenHandler.ValidateToken(request.accessToken, tokenValidationParameters, out SecurityToken validatedToken);

                if (validatedToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return BadRequest("Invalid token");
                }

                // Check if the access token has expired
                var expiryDateUnix = long.Parse(principal.Claims.First(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
                var expiryDateTimeUtc = DateTimeOffset.FromUnixTimeSeconds(expiryDateUnix).UtcDateTime;

                if (expiryDateTimeUtc > DateTime.UtcNow)
                {
                    return BadRequest("This token hasn't expired yet");
                }

                // Check if the refresh token exists in the database
                var refreshToken = await _context.Tokens.Find(x => x.RefreshToken == request.refreshToken).FirstOrDefaultAsync();
                if (refreshToken == null)
                {
                    return BadRequest("Invalid refresh token");
                }

                // Check if the access token's JTI matches the refresh token's JTI
                var jtiClaim = principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
                if (jtiClaim == null || jtiClaim != refreshToken.Jti)
                {
                    return BadRequest("Invalid refresh token");
                }

                // Generate a new access token
                var user = await _context.Users.Find(x => x.Username == refreshToken.Username).FirstOrDefaultAsync();
                if (user == null)
                {
                    return BadRequest("User not found");
                }

                var newAccessToken = _jwtService.GenerateAccessToken(user);

                // Delete old access token from cookies
                Response.Cookies.Delete("accessToken");
                // Delete old refresh token from database
                await _context.Tokens.DeleteOneAsync(x => x.RefreshToken == request.refreshToken);
                // Add new access token to cookies
                Response.Cookies.Append("accessToken", newAccessToken);
                // Add new refresh token to database
                var newRefreshToken = _jwtService.GenerateRefreshToken();
                await _context.Tokens.InsertOneAsync(new TokenData
                {
                    RefreshToken = newRefreshToken,
                    RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(5),
                    Username = user.Username,
                    Jti = Guid.NewGuid().ToString()
                });

                return Ok(new { accessToken = newAccessToken, 
                                refreshToken = newRefreshToken });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("handle-token")]
        public async Task<IActionResult> HandleRefreshToken([FromBody] RefreshTokenRequest request)
        {
            string refreshToken = request.RefreshToken;
            string userName = request.Username;
            try
            {
                var tokenData = await _tokenData.Find(t => t.Username == userName).FirstOrDefaultAsync();
                if (tokenData.RefreshTokensUsed.Contains(refreshToken))
                {
                    var filter = Builders<TokenData>.Filter.Eq(t => t.Username, userName);
                    await _tokenData.DeleteManyAsync(filter);
                    return BadRequest(new
                    {
                        message = "Có gì đó không ổn! Vui lòng đăng nhập lại"
                    });
                }
                var (newAccessToken, newRefreshToken) = await _jwtService.GenerateNewAccessToken(refreshToken, userName);

                return Ok(new
                {
                    accessToken = newAccessToken,
                    refreshToken = newRefreshToken
                }
                );
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
        [HttpGet("get-refresh-token")]
        public async Task<IActionResult> GetRefreshToken([FromBody] RefreshTokenRequest request)
        {
            var tokenData = await _jwtService.GetRefreshToken(request.RefreshToken);
            if (tokenData == null)
            {
                return BadRequest("Token not found!");
            }
            else
            {
                return Ok(tokenData);
            }
        }
    }
}
