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
        public string? KatilimKodu { get; set; }

        [Required]
        public string? OwnerId { get; set; } 

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        public bool AktifMi { get; set; } = true;

        public bool SistemdenBanliMi { get; set; } = false;
        public virtual ICollection<SinifKullanici> SinifKullanicilari { get; set; } = []; 
    }
}