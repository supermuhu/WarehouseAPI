using Microsoft.Extensions.DependencyInjection;
using WarehouseAPI.Services.Auth;
using WarehouseAPI.Services.Warehouse;
using WarehouseAPI.Services.Inbound;
using WarehouseAPI.Services.Pallet;
using WarehouseAPI.Services.Product;
using WarehouseAPI.Services.Outbound;

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

            // Inbound services
            services.AddScoped<IInboundService, InboundService>();

            // Pallet services
            services.AddScoped<IPalletService, PalletService>();

            // Product services
            services.AddScoped<IProductService, ProductService>();

            // Outbound services
            services.AddScoped<IOutboundService, OutboundService>();
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