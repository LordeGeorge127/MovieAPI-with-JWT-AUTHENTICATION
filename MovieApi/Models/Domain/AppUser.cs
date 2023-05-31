using Microsoft.AspNetCore.Identity;

namespace MovieApi.Models.Domain
{
    public class AppUser : IdentityUser
    {
        public string Name { get; set; }
    }
}
