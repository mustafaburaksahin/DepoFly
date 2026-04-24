using Microsoft.AspNetCore.Identity;

namespace DepoFly.Models
{
    // Sınıf içindeki lokal yetkileri tutacağımız Enum
    public enum SinifRolleri
    {
        Kurucu = 1,     // Sınıfı açan Admin
        GeciciAdmin = 2,// Admin'in yetki verdiği kişi
        Kullanici = 3   // Standart üye
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