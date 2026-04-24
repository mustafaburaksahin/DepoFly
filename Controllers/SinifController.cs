using DepoFly.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DepoFly.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DepoFly.Controllers
{
    [Authorize] // Sadece giriş yapan kullanıcılar erişebilir
    public class SinifController(AppDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly AppDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        // ==========================================
        // 1. SINIF OLUŞTURMA İŞLEMLERİ (SADECE SUPERADMIN/ADMIN)
        // ==========================================

        [Authorize(Roles = "SuperAdmin, Admin")]
        [HttpGet]
        public IActionResult Olustur()
        {
            return View();
        }

        [Authorize(Roles = "SuperAdmin, Admin")]
        [HttpPost]
        public async Task<IActionResult> Olustur(Sinif sinif)
        {
            ModelState.Remove("KatilimKodu");
            ModelState.Remove("OwnerId");

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                sinif.OwnerId = user.Id;

                // Rastgele 5 haneli benzersiz katılım kodu üretme
                sinif.KatilimKodu = "DFLY-" + Guid.NewGuid().ToString()[..5].ToUpper();

                _context.Siniflar.Add(sinif);
                await _context.SaveChangesAsync();

                // Sınıfı kuranı "Kurucu" rolüyle tabloya ekle
                var kurucuKaydi = new SinifKullanici
                {
                    SinifId = sinif.Id,
                    UserId = user.Id,
                    SinifİciRol = SinifRolleri.Kurucu
                };

                _context.SinifKullanicilari.Add(kurucuKaydi);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }
            return View(sinif);
        }

        // ==========================================
        // 2. SINIFA KATILMA İŞLEMLERİ (HER KULLANICI)
        // ==========================================

        [HttpGet]
        public IActionResult Katil()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Katil(string katilimKodu)
        {
            if (string.IsNullOrWhiteSpace(katilimKodu))
            {
                ModelState.AddModelError("", "Boş kod giremezsin!");
                return View();
            }

            katilimKodu = katilimKodu.Trim().ToUpper();
            var user = await _userManager.GetUserAsync(User);
            var sinif = await _context.Siniflar.FirstOrDefaultAsync(s => s.KatilimKodu == katilimKodu);

            if (sinif == null)
            {
                ModelState.AddModelError("", "Hatalı kod! Sınıf bulunamadı.");
                return View();
            }

            if (!sinif.AktifMi || sinif.SistemdenBanliMi)
            {
                ModelState.AddModelError("", "Bu sınıf şu an erişime kapalı.");
                return View();
            }

            var kayitKontrol = await _context.SinifKullanicilari
                .FirstOrDefaultAsync(sk => sk.SinifId == sinif.Id && sk.UserId == user.Id);

            if (kayitKontrol != null)
            {
                ModelState.AddModelError("", "Zaten bu sınıfa kayıtlısın.");
                return View();
            }

            var yeniUye = new SinifKullanici
            {
                SinifId = sinif.Id,
                UserId = user.Id,
                SinifİciRol = SinifRolleri.Kullanici
            };

            _context.SinifKullanicilari.Add(yeniUye);
            await _context.SaveChangesAsync();

            // Sınıfa katılır katılmaz depoya otomatik giriş yap
            HttpContext.Session.SetInt32("AktifSinifId", sinif.Id);
            TempData["BasariMesaji"] = "Sınıfa katıldın ve depoya giriş yapıldı!";

            return RedirectToAction("Index", "Urun");
        }

        // ==========================================
        // 3. SINIF YÖNETİM PANELİ
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Yonet(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            // AGA: .AsNoTracking() ekledik ki önbellekteki eski profil fotoları yerine DB'deki en güncel hali gelsin.
            var sinif = await _context.Siniflar
                .Include(s => s.SinifKullanicilari)
                    .ThenInclude(sk => sk.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sinif == null) return NotFound();

            var benimKaydim = sinif.SinifKullanicilari.FirstOrDefault(sk => sk.UserId == user.Id);

            // KULLANICI ROLÜNDE OLANLAR YÖNETİM SAYFASINA GİREMEZ
            if (benimKaydim == null || benimKaydim.SinifİciRol == SinifRolleri.Kullanici)
            {
                TempData["HataMesaji"] = "Yönetim yetkiniz yok!";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.BenimRolum = benimKaydim.SinifİciRol;
            return View(sinif);
        }

        // ==========================================
        // 4. DEPO GİRİŞ / ÇIKIŞ (SESSION) İŞLEMLERİ
        // ==========================================

        [HttpGet]
        public IActionResult DepoyaGir(int id)
        {
            HttpContext.Session.SetInt32("AktifSinifId", id);
            return RedirectToAction("Index", "Urun");
        }

        [HttpGet]
        public IActionResult KendiDepomaDon()
        {
            HttpContext.Session.Remove("AktifSinifId");
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 5. SINIF İÇİ KULLANICI İŞLEMLERİ
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YetkiDegistir(int sinifId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var yetkiKontrol = await _context.SinifKullanicilari
                .FirstOrDefaultAsync(sk => sk.SinifId == sinifId && sk.UserId == currentUserId && sk.SinifİciRol == SinifRolleri.Kurucu);

            if (yetkiKontrol == null && !User.IsInRole("SuperAdmin"))
                return Unauthorized("Bu işlem için yetkiniz yok.");

            var hedefKullanici = await _context.SinifKullanicilari
                .FirstOrDefaultAsync(sk => sk.SinifId == sinifId && sk.UserId == userId);

            if (hedefKullanici != null)
            {
                if (hedefKullanici.SinifİciRol == SinifRolleri.Kurucu)
                {
                    TempData["Hata"] = "Sınıf kurucusunun yetkisi değiştirilemez.";
                    return RedirectToAction("Yonet", new { id = sinifId });
                }

                hedefKullanici.SinifİciRol = hedefKullanici.SinifİciRol == SinifRolleri.Kullanici
                    ? SinifRolleri.GeciciAdmin
                    : SinifRolleri.Kullanici;

                await _context.SaveChangesAsync();
                TempData["Basari"] = "Kullanıcının sınıf içi yetkisi güncellendi.";
            }

            return RedirectToAction("Yonet", new { id = sinifId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SiniftanAt(int sinifId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var yetkiKontrol = await _context.SinifKullanicilari
                .FirstOrDefaultAsync(sk => sk.SinifId == sinifId && sk.UserId == currentUserId && sk.SinifİciRol == SinifRolleri.Kurucu);

            if (yetkiKontrol == null && !User.IsInRole("SuperAdmin"))
                return Unauthorized("Bu işlem için yetkiniz yok.");

            var hedefKullanici = await _context.SinifKullanicilari
                .FirstOrDefaultAsync(sk => sk.SinifId == sinifId && sk.UserId == userId);

            if (hedefKullanici != null)
            {
                if (hedefKullanici.SinifİciRol == SinifRolleri.Kurucu)
                {
                    TempData["Hata"] = "Sınıf kurucusunu sınıftan atamazsın.";
                    return RedirectToAction("Yonet", new { id = sinifId });
                }

                _context.SinifKullanicilari.Remove(hedefKullanici);
                await _context.SaveChangesAsync();
                TempData["Basari"] = "Kullanıcı sınıftan kalıcı olarak atıldı.";
            }

            return RedirectToAction("Yonet", new { id = sinifId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SiniftanBanla(int sinifId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var yapanKullanici = await _context.SinifKullanicilari
                .FirstOrDefaultAsync(sk => sk.SinifId == sinifId && sk.UserId == currentUserId);

            bool yetkiliMi = yapanKullanici != null && (yapanKullanici.SinifİciRol == SinifRolleri.Kurucu || yapanKullanici.SinifİciRol == SinifRolleri.GeciciAdmin);

            if (!yetkiliMi && !User.IsInRole("SuperAdmin"))
                return Unauthorized("Bu işlem için yetkiniz yok.");

            var hedefKullanici = await _context.SinifKullanicilari
                .FirstOrDefaultAsync(sk => sk.SinifId == sinifId && sk.UserId == userId);

            if (hedefKullanici != null)
            {
                if (hedefKullanici.SinifİciRol == SinifRolleri.Kurucu)
                {
                    TempData["Hata"] = "Sınıf kurucusunu banlayamazsın.";
                    return RedirectToAction("Yonet", new { id = sinifId });
                }

                if (yapanKullanici?.SinifİciRol == SinifRolleri.GeciciAdmin && hedefKullanici.SinifİciRol != SinifRolleri.Kullanici)
                {
                    TempData["Hata"] = "Sadece normal kullanıcıları banlayabilirsin.";
                    return RedirectToAction("Yonet", new { id = sinifId });
                }

                hedefKullanici.SınıftanBanliMi = !hedefKullanici.SınıftanBanliMi;
                await _context.SaveChangesAsync();

                TempData["Basari"] = hedefKullanici.SınıftanBanliMi ? "Kullanıcı bu sınıftan banlandı." : "Kullanıcının banı kaldırıldı.";
            }

            return RedirectToAction("Yonet", new { id = sinifId });
        }

        // ==========================================
        // 6. SINIF SİLME İŞLEMİ (SADECE KURUCU YAPABİLİR)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SinifSil(int sinifId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var yetkiKontrol = await _context.SinifKullanicilari
                .FirstOrDefaultAsync(sk => sk.SinifId == sinifId && sk.UserId == currentUserId && sk.SinifİciRol == SinifRolleri.Kurucu);

            if (yetkiKontrol == null && !User.IsInRole("SuperAdmin"))
            {
                TempData["HataMesaji"] = "Bu sınıfı silmek için yetkiniz yok!";
                return RedirectToAction("Index", "Home");
            }

            var silinecekSinif = await _context.Siniflar.FindAsync(sinifId);
            if (silinecekSinif != null)
            {
                _context.Siniflar.Remove(silinecekSinif);
                await _context.SaveChangesAsync();

                TempData["BasariMesaji"] = "Sınıf ve içindeki tüm veriler silindi.";

                var aktifSinifId = HttpContext.Session.GetInt32("AktifSinifId");
                if (aktifSinifId == sinifId)
                {
                    HttpContext.Session.Remove("AktifSinifId");
                }
            }

            return RedirectToAction("Index", "Home");
        }
    }
}