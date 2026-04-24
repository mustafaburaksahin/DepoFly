using System.ComponentModel.DataAnnotations;

namespace DepoFly.Models
{
    public class IletisimMesaj
    {
        [Required(ErrorMessage = "Ad Soyad boş bırakılamaz.")]
        public string AdSoyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email adresini yazmalısın.")]
        [EmailAddress(ErrorMessage = "Bu mail adresi pek gerçekçi durmuyor, lütfen tekrar deneyiniz.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mesaj Kısmı Boş Kaldı..")]
        [MinLength(10, ErrorMessage = "Mesajın çok kısa.")]
        public string Mesaj { get; set; } = string.Empty;
    }
}