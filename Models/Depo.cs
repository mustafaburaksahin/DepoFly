using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // ICollection için gerekli

namespace DepoFly.Models
{
    public class Depo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Depo adı boş bırakılamaz.")]
        [Display(Name = "Depo Adı")]
        public string? Ad { get; set; }

        public string? Konum { get; set; } // Opsiyonel: Adres veya raf bilgisi

        // Bu depoyu hangi kullanıcı yönetiyor?
        public string? UserId { get; set; }

        // MÜDAHALE BURADA: IdentityUser yerine ApplicationUser koyduk.
        // Hata veren son parça buydu.
        public virtual ApplicationUser? User { get; set; }

        // Bu depoda bulunan ürünlerin listesi
        public virtual ICollection<Urun>? Urunler { get; set; }

        public int? SinifId { get; set; }
    }
}