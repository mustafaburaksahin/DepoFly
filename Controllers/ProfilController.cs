using DepoFly.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DepoFly.Data;

namespace DepoFly.Controllers
{
    [Authorize]
    public class ProfilController(
        UserManager<ApplicationUser> userManager, // AGA: IdentityUser -> ApplicationUser yapıldı
        SignInManager<ApplicationUser> signInManager, // AGA: IdentityUser -> ApplicationUser yapıldı
        IWebHostEnvironment env,
        IEmailSender emailSender,
        AppDbContext context) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly IWebHostEnvironment _env = env;
        private readonly IEmailSender _emailSender = emailSender;
        private readonly AppDbContext _context = context;

        // --- PROFİL ANA SAYFA ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var notlar = _context.Set<DepoFly.Models.Not>()
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.Tarih)
                .ToList();

            var model = new ProfilViewModel { KullaniciAdi = user.UserName, Notlar = notlar };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(ProfilViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // --- PROFİL FOTOĞRAFI GÜNCELLEME ---
            if (!string.IsNullOrEmpty(model.CroppedImageBase64))
            {
                var base64Data = model.CroppedImageBase64.Split(',')[1];
                var imageBytes = Convert.FromBase64String(base64Data);

                // Dosya adı: kullanici_id.jpg
                var fileName = user.Id + ".jpg";
                var filePath = Path.Combine(_env.WebRootPath, "img", "profil", fileName);

                // Klasör yoksa oluştur aga
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory!);

                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                // Veritabanındaki ProfilFotoUrl alanını güncelle
                user.ProfilFotoUrl = "/img/profil/" + fileName;
            }

            user.UserName = model.KullaniciAdi;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Mesaj"] = "Profilin güncellendi!";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // --- ŞİFRE DEĞİŞTİRME SAYFASI (GET) ---
        [HttpGet]
        public async Task<IActionResult> SifreDegistir()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!await _userManager.HasPasswordAsync(user)) return RedirectToAction("SifreOlustur");
            return View();
        }

        // --- 1. ADIM: ONAY KODU GÖNDER (AJAX) ---
        [HttpPost]
        public async Task<IActionResult> OnayKoduGonder(string eskiSifre)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return BadRequest("Kullanıcı bulunamadı.");

            var checkPassword = await _userManager.CheckPasswordAsync(user, eskiSifre);
            if (!checkPassword) return BadRequest("Mevcut şifren yanlış");

            Random rnd = new();
            string onayKodu = rnd.Next(100000, 999999).ToString();

            TempData["OnayKodu"] = onayKodu;
            TempData.Save();

            try
            {
                await _emailSender.SendEmailAsync(user.Email!, "DepoFly Şifre Onay Kodu",
                    $"Şifreni değiştirmek için onay kodun: <h2>{onayKodu}</h2>");
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Mail hatası: " + ex.Message);
            }
        }

        // --- 2. ADIM: ŞİFREYİ ONAYLA VE GÜNCELLE (POST) ---
        [HttpPost]
        public async Task<IActionResult> SifreDegistir(SifreDegistirViewModel model, string girilenOnayKodu)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var beklenenKod = TempData.Peek("OnayKodu")?.ToString();

            if (string.IsNullOrEmpty(girilenOnayKodu) || girilenOnayKodu != beklenenKod)
            {
                ModelState.AddModelError("", "Girdiğin onay kodu yanlış veya süresi dolmuş.");
                return View(model);
            }

            if (model.YeniSifre != model.YeniSifreTekrar)
            {
                ModelState.AddModelError("", "Yeni şifreler uyuşmuyor.");
                return View(model);
            }

            var result = await _userManager.ChangePasswordAsync(user, model.EskiSifre!, model.YeniSifre!);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Mesaj"] = "Şifren başarıyla güncellendi!";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Notlar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var notlar = _context.Set<DepoFly.Models.Not>()
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.Tarih)
                .ToList();

            return View(notlar);
        }

        [HttpPost]
        public async Task<IActionResult> NotEkle(string icerik)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrEmpty(icerik)) return BadRequest();

            var yeniNot = new DepoFly.Models.Not { UserId = user.Id, Icerik = icerik, Tarih = DateTime.Now };
            _context.Set<DepoFly.Models.Not>().Add(yeniNot);
            await _context.SaveChangesAsync();

            return Ok(new { id = yeniNot.Id, icerik = yeniNot.Icerik, tarih = yeniNot.Tarih.ToString("dd.MM.yyyy HH:mm") });
        }

        [HttpPost]
        public async Task<IActionResult> NotSil(int id)
        {
            var not = await _context.Set<DepoFly.Models.Not>().FindAsync(id);
            if (not == null) return NotFound();

            _context.Set<DepoFly.Models.Not>().Remove(not);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> SifreOlustur()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (await _userManager.HasPasswordAsync(user)) return RedirectToAction("SifreDegistir");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SifreOlustur(string yeniSifre)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.AddPasswordAsync(user, yeniSifre!);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Mesaj"] = "Şifren oluşturuldu!";
                return RedirectToAction("Index");
            }
            return View();
        }
    }
}