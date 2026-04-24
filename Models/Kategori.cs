using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // ICollection için şart

namespace DepoFly.Models
{
    public class Kategori
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı boş bırakılamaz.")]
        public string? Ad { get; set; }

        // Kategoriyi hangi kullanıcının eklediğini bilmek için
        public string? UserId { get; set; }

        //IdentityUser'ı uçurduk, yerine ApplicationUser koyduk.
        // Hata tam olarak buradaydı!
        [ValidateNever]
        public virtual ApplicationUser? User { get; set; }

        [ValidateNever]
        public virtual ICollection<Urun>? Urunler { get; set; }

        public int? SinifId { get; set; }
    }
}