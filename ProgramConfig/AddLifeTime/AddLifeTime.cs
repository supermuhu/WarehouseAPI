using Microsoft.Extensions.DependencyInjection;
using RentalCarAPI.Services;
using RentalCarAPI.Services.Auth;
using RentalCarAPI.Services.Vehicle;
using RentalCarAPI.Services.User;
using RentalCarAPI.Services.VehicleType;
using RentalCarAPI.Services.VehicleStatus;
using RentalCarAPI.Services.MaintenanceHistory;
using RentalCarAPI.Services.PaymentMethod;
using RentalCarAPI.Services.PaymentStatus;
using RentalCarAPI.Services.Location;
using RentalCarAPI.Services.Route;
using RentalCarAPI.Services.Region;
using RentalCarAPI.Services.Booking;
using RentalCarAPI.Services.Payment;
using RentalCarAPI.Services.GenerateToken;
using RentalCarAPI.Services.BookingItem;
using RentalCarAPI.Services.RouteLocation;
using RentalCarAPI.Services.Role;
using RentalCarAPI.Services.UserStatus;
using RentalCarAPI.Services.BookingStatus;
using RentalCarAPI.Services.MaintenanceStatus;
using RentalCarAPI.Services.Contract;
using RentalCarAPI.Services.ContractHistory;
using RentalCarAPI.Services.Image;
using RentalCarAPI.Services.RentalType;


namespace RentalCarAPI.ProgramConfig
{
    public static class AddLifeTime
    {
        public static void AddScoped(this IServiceCollection services)
        {
            // Core services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserServiceOptimized>();
            services.AddScoped<IGenerateTokenService, GenerateTokenService>();
            services.AddScoped<IImageService, ImageService>();

            // Vehicle related services
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<IVehicleTypeService, VehicleTypeService>();
            services.AddScoped<IVehicleStatusService, VehicleStatusService>();
            services.AddScoped<IMaintenanceHistoryService, MaintenanceHistoryService>();

            // Location and region services
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<IRouteService, RouteService>();
            services.AddScoped<IRegionService, RegionService>();

            // Booking and payment services
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IBookingItemService, BookingItemService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IPaymentMethodService, PaymentMethodService>();
            services.AddScoped<IPaymentStatusService, PaymentStatusService>();
            services.AddScoped<IRouteLocationService, RouteLocationService>();

            // Contract services
            services.AddScoped<IContractService, ContractService>();
            services.AddScoped<IContractHistoryService, ContractHistoryService>();

            // Lookup table services
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IUserStatusService, UserStatusService>();
            // Rental type services
            services.AddScoped<IRentalTypeService, RentalTypeService>();

            services.AddScoped<IBookingStatusService, BookingStatusService>();
            services.AddScoped<IMaintenanceStatusService, MaintenanceStatusService>();
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