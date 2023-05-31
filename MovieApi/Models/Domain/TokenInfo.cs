namespace MovieApi.Models.Domain
{
    public class TokenInfo
    {
        public int id { get; set; }
        public string UserName { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiry { get; set;}
    }
}
