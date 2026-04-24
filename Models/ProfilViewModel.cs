using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic; // List kullanabilmek için şart

namespace DepoFly.Models
{
    public class ProfilViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı boş bırakılamaz!")]
        // Sadece harf, rakam, boşluk ve izin verdiğimiz temel işaretler
        [RegularExpression(@"^[a-zA-Z0-9\-._@+ ğüşıöçĞÜŞİÖÇ]+$", ErrorMessage = "Sadece harf, rakam, boşluk ve temel işaretleri (-._@+) kullanabilirsin!")]
        public string? KullaniciAdi { get; set; }

        public IFormFile? ProfilFotorafi { get; set; }

        // BUNU EKLEDİK: Kırpılan resmi metin (Base64) olarak tutacak
        public string? CroppedImageBase64 { get; set; }

        // NOTLAR LİSTESİ: Sayfada notları döngüyle basabilmek için burası kritik
         public List<Not> Notlar { get; set; } = [];
   }
}