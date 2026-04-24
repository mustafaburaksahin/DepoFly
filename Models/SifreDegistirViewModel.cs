using System.ComponentModel.DataAnnotations;

namespace DepoFly.Models
{
    public class SifreDegistirViewModel
    {
        [Required(ErrorMessage = "Mevcut şifreni girmelisin.")]
        [DataType(DataType.Password)]
        public string? EskiSifre { get; set; }

        [Required(ErrorMessage = "Yeni şifre boş olamaz.")]
        [DataType(DataType.Password)]
        public string? YeniSifre { get; set; }

        [Required(ErrorMessage = "Şifre tekrarı boş olamaz.")]
        [DataType(DataType.Password)]
        [Compare("YeniSifre", ErrorMessage = "Şifreler birbiriyle uyuşmuyor!")]
        public string? YeniSifreTekrar { get; set; }
    }
}