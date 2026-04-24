using Microsoft.AspNetCore.Identity;

namespace DepoFly.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? ProfilFotoUrl { get; set; }
    }
}