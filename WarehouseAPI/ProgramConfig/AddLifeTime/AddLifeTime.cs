using Microsoft.Extensions.DependencyInjection;
using WarehouseAPI.Services.Auth;
using WarehouseAPI.Services.Warehouse;

namespace WarehouseAPI.ProgramConfig
{
    public static class AddLifeTime
    {
        public static void AddScoped(this IServiceCollection services)
        {
            // Auth services
            services.AddScoped<IAuthService, AuthService>();

            // Warehouse services
            services.AddScoped<IWarehouseService, WarehouseService>();
        }
        
        public static void AddSingleton(this IServiceCollection services)
        {
            // services.AddSingleton<VNPayService>();
            // services.AddSingleton<EmailService>();
        }
        
        public static void AddTransient(this IServiceCollection services)
        {
            // services.AddTransient<EncryptionService>();
        }
    }
}