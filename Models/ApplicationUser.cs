using Microsoft.AspNetCore.Identity;

namespace DepoFly.Models
{
    // IdentityUser'dan türetiyoruz ki giriş-çıkış sistemin bozulmasın.
    public class ApplicationUser : IdentityUser
    {
        // İşte her yerde kullanacağımız o meşhur sütun
        public string? ProfilFotoUrl { get; set; }
    }
}