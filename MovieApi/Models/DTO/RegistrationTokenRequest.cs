namespace MovieApi.Models.DTO
{
    public class RegistrationTokenRequest
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set;}
    }
}
