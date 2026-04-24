using DepoFly.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DepoFly.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace DepoFly.Controllers
{
    [Authorize]
    public class KategoriController(AppDbContext db, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly AppDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        private int? AktifSinifId => HttpContext.Session.GetInt32("AktifSinifId");

        private string? GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        // YARDIMCI METOT: Sınıfta "Kullanıcı" rolündeyse true döner
        private async Task<bool> SadeceOkumaYetkisiVarMi()
        {
            if (!AktifSinifId.HasValue) return false;
            var userId = GetUserId();
            var sinifKaydi = await _db.SinifKullanicilari
                .FirstOrDefaultAsync(sk => sk.SinifId == AktifSinifId.Value && sk.UserId == userId);
            return sinifKaydi != null && sinifKaydi.SinifİciRol == SinifRolleri.Kullanici;
        }

        private bool AdminBireyselGirisYasakMi()
        {
            return (User.IsInRole("Admin") || User.IsInRole("SuperAdmin")) && !AktifSinifId.HasValue;
        }

        // 1. LİSTELEME (GÜNCELLENDİ: Bireysel/Sınıf Ayrımı Keskinleştirildi)
        public async Task<IActionResult> Index()
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");

            var userId = GetUserId();
            var kategoriler = _db.Kategori.AsQueryable();

            if (AktifSinifId.HasValue)
            {
                // AGA: Sınıf içindeysek sadece o sınıfa ait kategoriler
                kategoriler = kategoriler.Where(k => k.SinifId == AktifSinifId.Value);
            }
            else
            {
                // AGA: Bireysel alandaysak sadece kullanıcıya ait ve sınıfsız kategoriler
                kategoriler = kategoriler.Where(k => k.UserId == userId && k.SinifId == null);
            }

            ViewBag.SadeceOkuma = await SadeceOkumaYetkisiVarMi();
            return View(await kategoriler.ToListAsync());
        }

        // 2. Yeni Kategori - Sayfayı Açma
        public async Task<IActionResult> Create()
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));
            return View();
        }

        // 3. Yeni Kategori - Kaydetme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Kategori obj)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));

            obj.UserId = GetUserId();
            if (AktifSinifId.HasValue) obj.SinifId = AktifSinifId.Value;

            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Urunler");
            ModelState.Remove("SinifId");

            if (ModelState.IsValid)
            {
                _db.Kategori.Add(obj);
                await _db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        // 4. Düzenleme - Veriyi Getirme
        public async Task<IActionResult> Edit(int? id)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));
            if (id == null || id == 0) return NotFound();

            var userId = GetUserId();
            // AGA: Erişim kontrolü burada da ayrıldı
            var categoryFromDb = await _db.Kategori.FirstOrDefaultAsync(k => k.Id == id &&
                (AktifSinifId.HasValue ? k.SinifId == AktifSinifId.Value : (k.UserId == userId && k.SinifId == null)));

            if (categoryFromDb == null) return NotFound();

            return View(categoryFromDb);
        }

        // 5. Düzenleme - Güncelleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Kategori obj)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));

            var userId = GetUserId();
            var exists = await _db.Kategori.AnyAsync(k => k.Id == obj.Id &&
                (AktifSinifId.HasValue ? k.SinifId == AktifSinifId.Value : (k.UserId == userId && k.SinifId == null)));

            if (!exists) return NotFound();

            obj.UserId = userId;
            if (AktifSinifId.HasValue) obj.SinifId = AktifSinifId.Value;

            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Urunler");
            ModelState.Remove("SinifId");

            if (ModelState.IsValid)
            {
                _db.Kategori.Update(obj);
                await _db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        // 6. Silme - ONAY SAYFASI (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));
            if (id == null || id == 0) return NotFound();

            var userId = GetUserId();
            var categoryFromDb = await _db.Kategori.FirstOrDefaultAsync(k => k.Id == id &&
                (AktifSinifId.HasValue ? k.SinifId == AktifSinifId.Value : (k.UserId == userId && k.SinifId == null)));

            if (categoryFromDb == null) return NotFound();

            return View(categoryFromDb);
        }

        // 7. Silme - İŞLEM (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int? id)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));

            var userId = GetUserId();
            var categoryFromDb = await _db.Kategori.FirstOrDefaultAsync(k => k.Id == id &&
                (AktifSinifId.HasValue ? k.SinifId == AktifSinifId.Value : (k.UserId == userId && k.SinifId == null)));

            if (categoryFromDb == null) return NotFound();

            _db.Kategori.Remove(categoryFromDb);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}