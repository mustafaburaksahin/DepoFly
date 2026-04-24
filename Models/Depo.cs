using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace DepoFly.Models
{
    public class Depo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Depo adı boş bırakılamaz.")]
        [Display(Name = "Depo Adı")]
        public string? Ad { get; set; }

        public string? Konum { get; set; }
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<Urun>? Urunler { get; set; }

        public int? SinifId { get; set; }
    }
}