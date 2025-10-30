namespace WarehouseAPI.ModelView.Token
{
    public class RefreshTokenModel
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}
