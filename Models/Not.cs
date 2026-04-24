using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DepoFly.Models
{
    public class Not
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }

        [Required(ErrorMessage = "Not içeriği boş olamaz!")]
        [StringLength(500)]
        public string? Icerik { get; set; }

        public DateTime Tarih { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public int UrunId { get; internal set; }
    }
}