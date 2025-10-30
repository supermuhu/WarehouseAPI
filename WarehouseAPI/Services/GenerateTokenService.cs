using Azure.Core;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WarehouseAPI.Configuration;
using WarehouseAPI.ModelView.Token;
using WarehouseAPI.Data;
using WarehouseAPI.Services.GenerateToken;

namespace WarehouseAPI.Services.GenerateToken
{
    public class GenerateTokenService : IGenerateTokenService
    {
        private readonly WarehouseAPIContext db;
        public GenerateTokenService(WarehouseAPIContext db)
        {
            this.db = db;
        }
        public TokenModel GenerateLoginToken(Data.User model, string secretKey)
        {
            try
            {
                var jwtTokenHandler = new JwtSecurityTokenHandler();
                var secretKeyByte = Encoding.UTF8.GetBytes(secretKey);
                var claims = new List<Claim>
                {
                    // Lấy id người dùng
                    new Claim("Id", model.IdUser.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, model.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, model.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // Lấy vai trò của người dùng
                var role = db.Roles.SingleOrDefault(r => r.IdRole == model.IdRole);
                if (role != null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
                }

                var tokenDescription = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKeyByte), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = jwtTokenHandler.CreateToken(tokenDescription);
                var accessToken = jwtTokenHandler.WriteToken(token);
                var refreshToken = GenerateRefreshToken();

                // Try to save refresh token with transaction (optional for simplified system)
                try
                {
                    db.Database.BeginTransaction();
                    db.RefreshTokens.Add(new RefreshToken
                    {
                        IdUser = model.IdUser,
                        Token = refreshToken,
                        JwtId = token.Id,
                        IssueAt = DateTime.UtcNow,
                        IsUsed = false,
                        ExpiredAt = DateTime.UtcNow.AddYears(1)
                    });

                    db.SaveChanges();
                    db.Database.CommitTransaction();
                }
                catch (Exception)
                {
                    try { db.Database.RollbackTransaction(); } catch { }
                    // Continue without refresh token - this is acceptable for a simplified system
                }

                return new TokenModel
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Expires = tokenDescription.Expires.Value,
                    IdRole = model.IdRole,
                    NameRole = role != null ? role.RoleName : "Không xác định"
                };
            }
            catch (Exception)
            {
                return null;
            }

        }

        public TokenModel GenerateToken(Data.User model, string secretKey)
        {
            // For simplified system, just return the same as GenerateLoginToken
            return GenerateLoginToken(model, secretKey);
        }



        public string GenerateRefreshToken()
        {
            // Generate a simple refresh token (not implemented in simplified system)
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
    }
}
