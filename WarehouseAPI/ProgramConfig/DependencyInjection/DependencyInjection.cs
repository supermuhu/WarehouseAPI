// using WarehouseAPI.Configuration;

// namespace WarehouseAPI.ProgramConfig.DependencyInjection
// {
//     public static class DependencyInjection
//     {
//         public static void Config(this IServiceCollection services, IConfiguration configuration)
//         {
//             var appSettingsSection = configuration.GetSection("AppSettings");
//             services.Configure<AppSettings>(appSettingsSection);

//             var emailConfigurationSection = configuration.GetSection("EmailConfiguration");
//             services.Configure<EmailConfiguration>(emailConfigurationSection);

//             var VNPayConfigurationSection = configuration.GetSection("VNPay");
//             services.Configure<VNPayConfiguration>(VNPayConfigurationSection);

//             var HostConfigurationSection = configuration.GetSection("Host");
//             services.Configure<HostConfiguration>(HostConfigurationSection);
//         }
//     }
// }
