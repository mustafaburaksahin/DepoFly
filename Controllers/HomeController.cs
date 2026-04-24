using System.Diagnostics;
using DepoFly.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using DepoFly.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace DepoFly.Controllers
{
    // AGA: Primary Constructor içinde ApplicationUser'ı tanıttık
    public class HomeController(
        ILogger<HomeController> logger,
        AppDbContext context,
        IEmailSender emailSender,
        UserManager<ApplicationUser> userManager, // IdentityUser -> ApplicationUser yapıldı
        IMemoryCache memoryCache) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;
        private readonly AppDbContext _context = context;
        private readonly IEmailSender _emailSender = emailSender;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IMemoryCache _cache = memoryCache;

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return RedirectToAction("Index", "SuperAdmin");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);

                // Sınıfları ve profil fotoğraflarını getirebilmek için ApplicationUser bağlantısını koruyoruz
                var benimSiniflarim = await _context.SinifKullanicilari
                    .Include(sk => sk.Sinif)
                    .Where(sk => sk.UserId == userId && sk.Sinif.AktifMi)
                    .ToListAsync();

                return View(benimSiniflarim);
            }

            return View(new List<SinifKullanici>());
        }

        [HttpGet]
        public async Task<IActionResult> İstatistikleriGetir()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var aktifSinifId = HttpContext.Session.GetInt32("AktifSinifId");

                if (aktifSinifId.HasValue)
                {
                    return Json(new
                    {
                        urunSayisi = await _context.Urunler.CountAsync(u => u.SinifId == aktifSinifId.Value),
                        kategoriSayisi = await _context.Kategori.CountAsync(k => k.SinifId == aktifSinifId.Value),
                        depoSayisi = await _context.Depolar.CountAsync(d => d.SinifId == aktifSinifId.Value)
                    });
                }
                else
                {
                    return Json(new
                    {
                        urunSayisi = await _context.Urunler.CountAsync(u => u.UserId == userId && u.SinifId == null),
                        kategoriSayisi = await _context.Kategori.CountAsync(k => k.UserId == userId && k.SinifId == null),
                        depoSayisi = await _context.Depolar.CountAsync(d => d.UserId == userId && d.SinifId == null)
                    });
                }
            }

            return Json(new
            {
                urunSayisi = await _context.Urunler.CountAsync(),
                kategoriSayisi = await _context.Kategori.CountAsync(),
                depoSayisi = await _context.Depolar.CountAsync()
            });
        }

        [HttpGet]
        public IActionResult Iletisim()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Iletisim(IletisimMesaj model)
        {
            // --- 1. SPAM KONTROLÜ ---
            var ipAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "BilinmeyenIP";
            var cacheKey = $"iletisim_spam_{ipAdresi}";

            if (_cache.TryGetValue(cacheKey, out _))
            {
                ModelState.AddModelError(string.Empty, "Çok hızlı mesaj gönderiyorsun! Lütfen 2 dakika bekle.");
                return View(model);
            }

            // --- 2. RECAPTCHA ---
            var recaptchaResponse = Request.Form["g-recaptcha-response"];
            var secretKey = "6LdNSsIsAAAAAOWLD8t2gzWK8uPNr5-X9xr5EPIw";

            if (string.IsNullOrEmpty(recaptchaResponse))
            {
                ModelState.AddModelError(string.Empty, "Lütfen robot olmadığınızı doğrulayın.");
                return View(model);
            }

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={recaptchaResponse}", null);
                var jsonString = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(jsonString);
                if (!document.RootElement.GetProperty("success").GetBoolean())
                {
                    ModelState.AddModelError(string.Empty, "Bot doğrulaması başarısız.");
                    return View(model);
                }
            }

            // --- 3. MAİL GÖNDERME ---
            if (ModelState.IsValid)
            {
                try
                {
                    string mailIcerigi = $@"
                        <h3>DepoFly'dan Yeni Mesaj Var!</h3>
                        <hr>
                        <p><b>Gönderen:</b> {model.AdSoyad}</p>
                        <p><b>E-Posta:</b> {model.Email}</p>
                        <p><b>Mesaj:</b><br>{model.Mesaj}</p>
                        <hr>";

                    await _emailSender.SendEmailAsync("depofly.destek@gmail.com", "DepoFly Yeni İletişim Mesajı", mailIcerigi);

                    var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(2));
                    _cache.Set(cacheKey, true, cacheOptions);

                    TempData["Mesaj"] = "Mesajın başarıyla iletildi ! En kısa sürede döneceğiz.";
                    return RedirectToAction("Iletisim");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Mail hatası.");
                    ModelState.AddModelError("", "Mesaj gönderilemedi.");
                }
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}