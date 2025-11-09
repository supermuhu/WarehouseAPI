using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WarehouseAPI.Data;
using WarehouseAPI.Helpers;
using WarehouseAPI.ModelView.Auth;

namespace WarehouseAPI.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly WarehouseApiContext db;
        private readonly IConfiguration configuration;
        private readonly string secretKey;

        public AuthService(WarehouseApiContext db, IConfiguration configuration)
        {
            this.db = db;
            this.configuration = configuration;
            this.secretKey = configuration["AppSettings:SecretKey"] ?? "YourDefaultSecretKeyHere123456789012";
        }

        public ApiResponse Login(AuthSignInModel model)
        {
            try
            {
                // Tìm account theo email hoặc username
                var account = db.Accounts
                    .SingleOrDefault(u => u.Email == model.Email || u.Username == model.Email);

                if (account == null)
                {
                    return new ApiResponse(404, "Email hoặc username không hợp lệ", null);
                }

                // Verify password với BCrypt
                if (!BCrypt.Net.BCrypt.Verify(model.Password, account.PasswordHash))
                {
                    return new ApiResponse(401, "Mật khẩu không hợp lệ", null);
                }

                // Kiểm tra trạng thái account
                if (account.Status != "active")
                {
                    return new ApiResponse(403, "Tài khoản chưa được kích hoạt hoặc đã bị khóa", null);
                }

                // Tạo JWT token
                var token = GenerateJwtToken(account);
                if (token == null)
                {
                    return new ApiResponse(500, "Không thể tạo token", null);
                }

                return new ApiResponse(200, "Đăng nhập thành công", new
                {
                    accessToken = token,
                    accountId = account.AccountId,
                    username = account.Username,
                    fullName = account.FullName,
                    email = account.Email,
                    role = account.Role
                });
            }
            catch (Exception e)
            {
                return new ApiResponse(500, $"Lỗi server: {e.Message}", null);
            }
        }

        public ApiResponse Logout()
        {
            try
            {
                // Trong JWT stateless authentication, logout được xử lý ở client-side
                // bằng cách xóa token khỏi storage
                // Server chỉ trả về response thành công
                return new ApiResponse(200, "Đăng xuất thành công", null);
            }
            catch (Exception e)
            {
                return new ApiResponse(500, $"Lỗi server: {e.Message}", null);
            }
        }

        private string GenerateJwtToken(Account account)
        {
            try
            {
                var jwtTokenHandler = new JwtSecurityTokenHandler();
                var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);

                var claims = new List<Claim>
                {
                    new Claim("AccountId", account.AccountId.ToString()),
                    new Claim("Username", account.Username),
                    new Claim("Email", account.Email ?? ""),
                    new Claim("Role", account.Role),
                    new Claim("FullName", account.FullName ?? ""),
                    new Claim(JwtRegisteredClaimNames.Sub, account.Username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(secretKeyBytes),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = jwtTokenHandler.CreateToken(tokenDescriptor);
                return jwtTokenHandler.WriteToken(token);
            }
            catch
            {
                return null;
            }
        }
    }
}