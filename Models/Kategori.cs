using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; 

namespace DepoFly.Models
{
    public class Kategori
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı boş bırakılamaz.")]
        public string? Ad { get; set; }
        public string? UserId { get; set; }

        [ValidateNever]
        public virtual ApplicationUser? User { get; set; }

        [ValidateNever]
        public virtual ICollection<Urun>? Urunler { get; set; }

        public int? SinifId { get; set; }
    }
}