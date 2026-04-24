using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace DepoFly.Models
{
    public class ProfilViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı boş bırakılamaz!")]
        [RegularExpression(@"^[a-zA-Z0-9\-._@+ ğüşıöçĞÜŞİÖÇ]+$", ErrorMessage = "Sadece harf, rakam, boşluk ve temel işaretleri (-._@+) kullanabilirsin!")]
        public string? KullaniciAdi { get; set; }

        public IFormFile? ProfilFotorafi { get; set; }

        public string? CroppedImageBase64 { get; set; }

         public List<Not> Notlar { get; set; } = [];
   }
}