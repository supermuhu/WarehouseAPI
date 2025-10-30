using Microsoft.Extensions.DependencyInjection;
using WarehouseAPI.Services;
using WarehouseAPI.Services.Auth;
using WarehouseAPI.Services.User;
using WarehouseAPI.Services.GenerateToken;
using WarehouseAPI.Services.Role;
using WarehouseAPI.Services.UserStatus;
using WarehouseAPI.Services.GenerateToken;
using WarehouseAPI.Services;


namespace WarehouseAPI.ProgramConfig
{
    public static class AddLifeTime
    {
        public static void AddScoped(this IServiceCollection services)
        {
            // Core services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserServiceOptimized>();

            // Lookup table services
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IUserStatusService, UserStatusService>();
        }
        public static void AddSingleton(this IServiceCollection services)
        {
            services.AddSingleton<VNPayService>();
            services.AddSingleton<EmailService>();
        }
        public static void AddTransient(this IServiceCollection services)
        {
            services.AddTransient<EncryptionService>();
            services.AddTransient<GenerateTokenService>();
        }
    }
}