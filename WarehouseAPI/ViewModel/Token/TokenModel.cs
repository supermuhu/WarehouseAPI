namespace WarehouseAPI.ModelView.Token
{
    public class TokenModel
    {
        public string AccessToken { get; set; } = null!;
        public string? RefreshToken { get; set; }
        public string JwtId { get; set; } = null!;
        public DateTime ExpiryDate { get; set; }
        public DateTime Expires { get; set; }
        public int IdRole { get; set; }
        public string NameRole { get; set; } = null!;
    }
}
