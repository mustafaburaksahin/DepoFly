using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace DepoFly.Models
{
    public class Urun
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ürün adı boş bırakılamaz.")]
        public string? Ad { get; set; }

        public string? Barkod { get; set; }

        [Range(0, 1000000, ErrorMessage = "Stok miktarı 0 ile 1.000.000 arasında olmalıdır.")]
        public int StokMiktari { get; set; }

        public decimal BirimFiyat { get; set; }

        public DateTime KayitTarihi { get; set; } = DateTime.Now;

        public string? UserId { get; set; }

        [ValidateNever]
        //IdentityUser'ı sildik, yerine ApplicationUser koyduk.
        public virtual ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "Lütfen bir ürün türü seçin.")]
        public int KategoriId { get; set; }

        [ValidateNever]
        public virtual Kategori? Kategori { get; set; }

        [Required(ErrorMessage = "Lütfen bir depo seçin.")]
        public int DepoId { get; set; }

        [ValidateNever]
        public virtual Depo? Depo { get; set; }

        public int? SinifId { get; set; }
    }
}