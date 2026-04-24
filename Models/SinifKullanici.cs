using Microsoft.AspNetCore.Identity;

namespace DepoFly.Models
{
    public enum SinifRolleri
    {
        Kurucu = 1,     
        GeciciAdmin = 2,
        Kullanici = 3   
    }

    public class SinifKullanici
    {
        public int SinifId { get; set; }
        public Sinif? Sinif { get; set; } 

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public SinifRolleri SinifİciRol { get; set; } = SinifRolleri.Kullanici;
        public bool SınıftanBanliMi { get; set; } = false;
    }
}