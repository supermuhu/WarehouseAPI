using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.Helpers;
using WarehouseAPI.ModelView.Auth;
using WarehouseAPI.ModelView.Token;
using WarehouseAPI.ModelView.User;
using WarehouseAPI.Services.GenerateToken;

namespace WarehouseAPI.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly WarehouseAPIContext db;
        private readonly IGenerateTokenService generateTokenService;
        private readonly string secretKey;

        public AuthService(WarehouseAPIContext db, IGenerateTokenService generateTokenService, IConfiguration configuration)
        {
            this.db = db;
            this.generateTokenService = generateTokenService;
            this.secretKey = configuration["AppSettings:SecretKey"];
        }

        public ApiResponse Login(AuthSignInModel model)
        {
            try
            {
                var user = db.Users
                    .Include(u => u.IdRoleNavigation)
                    .Include(u => u.IdUserStatusNavigation)
                    .SingleOrDefault(u => u.Email == model.Email);

                if (user == null)
                {
                    return new ApiResponse(404, "Email không hợp lệ", null);
                }

                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    return new ApiResponse(404, "Mật khẩu không hợp lệ", null);
                }

                if (user.IdUserStatusNavigation.StatusName != "Active")
                {
                    return new ApiResponse(403, "Tài khoản chưa được kích hoạt", null);
                }

                var token = generateTokenService.GenerateLoginToken(user, secretKey);
                if (token == null)
                {
                    return new ApiResponse(402, "Không thể tạo token", null);
                }

                return new ApiResponse(200, "Đăng nhập thành công", token);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse SignUp(AuthSignUpModel model)
        {
            try
            {
                // Check if email already exists
                var existingUser = db.Users.SingleOrDefault(x => x.Email == model.Email);
                if (existingUser != null)
                {
                    return new ApiResponse(403, "Email đã tồn tại", null);
                }

                // Check if passwords match
                if (model.Password != model.RepeatPassword)
                {
                    return new ApiResponse(403, "Mật khẩu và mật khẩu nhập lại không khớp", null);
                }

                // Check if ID card number already exists
                var existingIdCard = db.Users.SingleOrDefault(x => x.IdCardNumber == model.IdCardNumber);
                if (existingIdCard != null)
                {
                    return new ApiResponse(403, "Số CMND/CCCD đã tồn tại", null);
                }

                var newUser = new Data.User
                {
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Address = model.Address,
                    IdCardNumber = model.IdCardNumber,
                    DriverLicense = model.DriverLicense,
                    IdRole = 2, // Customer role
                    IdUserStatus = 1, // Active status (simplified - no OTP verification)
                    CreatedAt = DateTime.Now
                };

                db.Users.Add(newUser);
                db.SaveChanges();

                return new ApiResponse(200, "Đăng ký người dùng thành công", null);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse Logout(AuthEmailModel model)
        {
            try
            {
                // In a simplified system, logout is handled client-side by removing the token
                // No server-side token invalidation needed without refresh tokens
                return new ApiResponse(200, "Đăng xuất thành công", null);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse VerifySignUpOTP(AuthVerifyOTPModel model)
        {
            // OTP verification removed in simplified system
            return new ApiResponse(501, "OTP verification not implemented in simplified system", null);
        }

        public ApiResponse VerifyForgotPasswordOTP(AuthVerifyOTPModel model)
        {
            // OTP verification removed in simplified system
            return new ApiResponse(501, "OTP verification not implemented in simplified system", null);
        }

        public ApiResponse ResendOTP(AuthEmailModel model)
        {
            // OTP functionality removed in simplified system
            return new ApiResponse(501, "OTP functionality not implemented in simplified system", null);
        }

        public ApiResponse ForgotPassword(AuthForgotPasswordModel model)
        {
            try
            {
                var user = db.Users.SingleOrDefault(x => x.Email == model.Email);
                if (user == null)
                {
                    return new ApiResponse(404, "Không tìm thấy người dùng", null);
                }

                // Simple password reset - update password directly
                user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                db.SaveChanges();

                return new ApiResponse(200, "Đặt lại mật khẩu thành công", null);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse RefreshToken(RefreshTokenModel model)
        {
            // Refresh token functionality removed in simplified system
            return new ApiResponse(501, "Refresh token not implemented in simplified system", null);
        }
        public ApiResponse Update(int id, UserUpdateModel model)
        {
            try
            {
                var user = db.Users.FirstOrDefault(u => u.IdUser == id);
                if (user == null)
                {
                    return new ApiResponse(404, "Không tìm thấy người dùng", null);
                }

                if (!string.IsNullOrEmpty(model.FullName))
                    user.FullName = model.FullName;

                if (!string.IsNullOrEmpty(model.Phone))
                    user.Phone = model.Phone;

                if (!string.IsNullOrEmpty(model.Address))
                    user.Address = model.Address;
                if(!string.IsNullOrEmpty(model.IdCardNumber))
                    user.IdCardNumber = model.IdCardNumber;
                if (!string.IsNullOrEmpty(model.DriverLicense))
                    user.DriverLicense = model.DriverLicense;

                if (model.LicenseExpiryDate.HasValue)
                    user.DriverLicenseExpiry = model.LicenseExpiryDate.Value;

                if (model.IdUserStatus.HasValue)
                    user.IdUserStatus = model.IdUserStatus.Value;

                if (model.IdRole.HasValue)
                    user.IdRole = model.IdRole.Value;

                if (!string.IsNullOrEmpty(model.Password))
                    user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

                db.SaveChanges();

                return new ApiResponse(200, "Cập nhật người dùng thành công", null);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }
    }
}