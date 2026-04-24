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
    public class DepoController(AppDbContext db, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly AppDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        // Session'daki Aktif Sınıf ID'sini çeker
        private int? AktifSinifId => HttpContext.Session.GetInt32("AktifSinifId");

        private string? GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        // YARDIMCI METOT: Sınıfta "Kullanici" rolündeyse sadece okuma yapabilir
        private async Task<bool> SadeceOkumaYetkisiVarMi()
        {
            if (!AktifSinifId.HasValue) return false;
            var userId = GetUserId();
            var sinifKaydi = await _db.SinifKullanicilari
                .FirstOrDefaultAsync(sk => sk.SinifId == AktifSinifId.Value && sk.UserId == userId);

            return sinifKaydi != null && sinifKaydi.SinifİciRol == SinifRolleri.Kullanici;
        }

        // GÜVENLİK BARİKATI: Adminlerin bireysel depo yönetimine girmesini engeller
        private bool AdminBireyselGirisYasakMi()
        {
            return (User.IsInRole("Admin") || User.IsInRole("SuperAdmin")) && !AktifSinifId.HasValue;
        }

        // 1. LİSTELEME (GÜNCELLENDİ: Sınıf/Bireysel Ayrımı Keskinleştirildi)
        public async Task<IActionResult> Index()
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");

            var userId = GetUserId();
            var depolar = _db.Depolar.AsQueryable();

            if (AktifSinifId.HasValue)
            {
                // AGA: Sınıf içindeysek sadece o sınıfa ait depolar
                depolar = depolar.Where(d => d.SinifId == AktifSinifId.Value);
            }
            else
            {
                // AGA: Bireysel depodaysak sadece kullanıcıya ait ve sınıfsız depolar
                depolar = depolar.Where(d => d.UserId == userId && d.SinifId == null);
            }

            ViewBag.SadeceOkuma = await SadeceOkumaYetkisiVarMi();
            return View(await depolar.ToListAsync());
        }

        // 2. EKLEME (SAYFAYI AÇAR)
        public async Task<IActionResult> Create()
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));
            return View();
        }

        // 3. EKLEME (KAYDETME)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Depo obj)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));

            obj.UserId = GetUserId();
            if (AktifSinifId.HasValue) obj.SinifId = AktifSinifId.Value;

            ModelState.Remove("User");
            ModelState.Remove("Urunler");
            ModelState.Remove("UserId");
            ModelState.Remove("SinifId");

            if (ModelState.IsValid)
            {
                _db.Depolar.Add(obj);
                await _db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        // 4. DÜZENLEME (SAYFAYI AÇAR)
        public async Task<IActionResult> Edit(int? id)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));
            if (id == null || id == 0) return NotFound();

            var userId = GetUserId();
            // AGA: Erişim kontrolü güncellendi
            var depo = await _db.Depolar.FirstOrDefaultAsync(d => d.Id == id &&
                (AktifSinifId.HasValue ? d.SinifId == AktifSinifId.Value : (d.UserId == userId && d.SinifId == null)));

            if (depo == null) return NotFound();
            return View(depo);
        }

        // 5. DÜZENLEME (GÜNCELLEME)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Depo obj)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));

            var userId = GetUserId();
            var exists = await _db.Depolar.AnyAsync(d => d.Id == obj.Id &&
                (AktifSinifId.HasValue ? d.SinifId == AktifSinifId.Value : (d.UserId == userId && d.SinifId == null)));

            if (!exists) return NotFound();

            obj.UserId = userId;
            if (AktifSinifId.HasValue) obj.SinifId = AktifSinifId.Value;

            ModelState.Remove("User");
            ModelState.Remove("Urunler");
            ModelState.Remove("UserId");
            ModelState.Remove("SinifId");

            if (ModelState.IsValid)
            {
                _db.Depolar.Update(obj);
                await _db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        // 6. SİLME (ONAY SAYFASI)
        public async Task<IActionResult> Delete(int? id)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));
            if (id == null || id == 0) return NotFound();

            var userId = GetUserId();
            var depo = await _db.Depolar.FirstOrDefaultAsync(d => d.Id == id &&
                (AktifSinifId.HasValue ? d.SinifId == AktifSinifId.Value : (d.UserId == userId && d.SinifId == null)));

            if (depo == null) return NotFound();
            return View(depo);
        }

        // 7. SİLME (İŞLEM)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int? id)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));

            var userId = GetUserId();
            var depo = await _db.Depolar.FirstOrDefaultAsync(d => d.Id == id &&
                (AktifSinifId.HasValue ? d.SinifId == AktifSinifId.Value : (d.UserId == userId && d.SinifId == null)));

            if (depo == null) return NotFound();

            _db.Depolar.Remove(depo);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}