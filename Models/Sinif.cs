using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace DepoFly.Models
{
    public class Sinif
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Sınıf adı boş bırakılamaz!")]
        [StringLength(100)]
        [Display(Name = "Sınıf Adı")]
        public string? SinifAdi { get; set; }

        [StringLength(500)]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }

        [Required]
        [StringLength(10)]
        [Display(Name = "Katılım Kodu")]
        public string? KatilimKodu { get; set; } // Örn: DFLY-456

        [Required]
        public string? OwnerId { get; set; } // Sınıfı oluşturan (Kurucu) Admin'in Id'si

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        public bool AktifMi { get; set; } = true;

        // --- BURASI EKSİKTİ, EKLENDİ ---
        public bool SistemdenBanliMi { get; set; } = false;

        // --- YENİ EKLENEN: İLİŞKİLER (Navigation Properties) ---

        // Sınıfa katılan tüm kullanıcılar ve onların bu sınıfa özel rolleri (Geçici Admin, Banlı vs.)
        public virtual ICollection<SinifKullanici> SinifKullanicilari { get; set; } = [];

        // İleride bu sınıfın ortak deposuna ürün eklemek istersen burayı açarsın:
        // public virtual ICollection<Urun> Urunler { get; set; } 
    }
}