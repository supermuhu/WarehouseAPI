namespace WarehouseAPI.ModelView.Auth
{
    public class AuthSignUpModel
    {
        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string RepeatPassword { get; set; } = null!;

        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string IdCardNumber { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? DriverLicense { get; set; }
    }
}