using Microsoft.AspNetCore.Authorization;

namespace RentalCarAPI.ProgramConfig.Policy
{
    public static class AuthorizationPolicy
    {
        public static void AddCustomPolicies(this AuthorizationOptions options)
        {
            options.AddPolicy("DefaultPolicy", policy =>
                policy.RequireAuthenticatedUser());
            //options.AddPolicy("read-event", policy =>
            //    policy.RequireAssertion(context =>
            //        context.User.HasClaim(c => c.Type == "Permission" && c.Value == "read-event")));
            
        }
    }
}
