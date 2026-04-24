using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DepoFly.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using DepoFly.Data;

namespace DepoFly.Controllers
{
    [Authorize]
    public class UrunController(AppDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly AppDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        private int? AktifSinifId => HttpContext.Session.GetInt32("AktifSinifId");

        // YARDIMCI METOT: Sınıfta "Kullanıcı" rolündeyse true döner
        private async Task<bool> SadeceOkumaYetkisiVarMi()
        {
            if (!AktifSinifId.HasValue) return false;

            var userId = _userManager.GetUserId(User);
            var sinifKaydi = await _context.SinifKullanicilari
                .FirstOrDefaultAsync(sk => sk.SinifId == AktifSinifId.Value && sk.UserId == userId);

            return sinifKaydi != null && sinifKaydi.SinifİciRol == SinifRolleri.Kullanici;
        }

        private bool AdminBireyselGirisYasakMi()
        {
            return (User.IsInRole("Admin") || User.IsInRole("SuperAdmin")) && !AktifSinifId.HasValue;
        }

        // 1. LİSTELEME (GÜNCELLENDİ: Bireysel/Sınıf Ayrımı Keskinleştirildi)
        public async Task<IActionResult> Index(string arananKelime)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");

            var userId = _userManager.GetUserId(User);
            var urunler = _context.Urunler.Include(u => u.Kategori).Include(u => u.Depo).AsQueryable();

            if (AktifSinifId.HasValue)
            {
                
                urunler = urunler.Where(u => u.SinifId == AktifSinifId.Value);
            }
            else
            {
                
                urunler = urunler.Where(u => u.UserId == userId && u.SinifId == null);
            }

            if (!string.IsNullOrEmpty(arananKelime))
                urunler = urunler.Where(u => u.Ad!.Contains(arananKelime) || u.Barkod!.Contains(arananKelime));

            ViewBag.SadeceOkuma = await SadeceOkumaYetkisiVarMi();
            var liste = await urunler.ToListAsync();

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                return PartialView("_UrunListesiPartial", liste);

            return View(liste);
        }

        // LİSTELERİ YÜKLE (GÜNCELLENDİ: Dropdown'larda veri sızıntısı engellendi)
        private void ListeleriYukle()
        {
            var userId = _userManager.GetUserId(User);
            if (AktifSinifId.HasValue)
            {
                // Sadece aktif sınıfa ait kategori ve depolar
                ViewBag.KategoriListesi = _context.Kategori
                    .Where(k => k.SinifId == AktifSinifId.Value)
                    .Select(u => new SelectListItem { Text = u.Ad, Value = u.Id.ToString() }).ToList();

                ViewBag.DepoListesi = _context.Depolar
                    .Where(d => d.SinifId == AktifSinifId.Value)
                    .Select(d => new SelectListItem { Text = d.Ad, Value = d.Id.ToString() }).ToList();
            }
            else
            {
                // Sadece kullanıcıya ait ve sınıfa bağlı olmayan kategori ve depolar
                ViewBag.KategoriListesi = _context.Kategori
                    .Where(k => k.UserId == userId && k.SinifId == null)
                    .Select(u => new SelectListItem { Text = u.Ad, Value = u.Id.ToString() }).ToList();

                ViewBag.DepoListesi = _context.Depolar
                    .Where(d => d.UserId == userId && d.SinifId == null)
                    .Select(d => new SelectListItem { Text = d.Ad, Value = d.Id.ToString() }).ToList();
            }
        }

        public async Task<IActionResult> Create()
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));
            ListeleriYukle();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Urun urun)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));

            urun.UserId = _userManager.GetUserId(User);
            if (urun.KayitTarihi == default) urun.KayitTarihi = DateTime.Now;
            if (AktifSinifId.HasValue) urun.SinifId = AktifSinifId.Value;

            string[] silinecekler = ["KayitTarihi", "UserId", "Kategori", "Depo", "User", "SinifId"];
            foreach (var item in silinecekler) ModelState.Remove(item);

            if (ModelState.IsValid)
            {
                _context.Add(urun);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ListeleriYukle();
            return View(urun);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            // Filtreleme burada da bireysel/sınıf ayrımına göre yapılıyor
            var urun = await _context.Urunler.FirstOrDefaultAsync(u => u.Id == id &&
                (AktifSinifId.HasValue ? u.SinifId == AktifSinifId.Value : (u.UserId == userId && u.SinifId == null)));

            if (urun == null) return NotFound();

            ListeleriYukle();
            return View(urun);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Urun urun)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));
            if (id != urun.Id) return NotFound();

            var userId = _userManager.GetUserId(User);
            var gercekUrunExists = await _context.Urunler.AnyAsync(u => u.Id == id &&
                (AktifSinifId.HasValue ? u.SinifId == AktifSinifId.Value : (u.UserId == userId && u.SinifId == null)));

            if (!gercekUrunExists) return NotFound();

            urun.UserId = userId;
            if (AktifSinifId.HasValue) urun.SinifId = AktifSinifId.Value;

            string[] silinecekler = ["Kategori", "Depo", "User", "UserId", "KayitTarihi", "SinifId"];
            foreach (var item in silinecekler) ModelState.Remove(item);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(urun);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException) { if (!UrunExists(urun.Id)) return NotFound(); else throw; }
            }
            ListeleriYukle();
            return View(urun);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (id == null) return NotFound();
            var userId = _userManager.GetUserId(User);
            var urun = await _context.Urunler.Include(u => u.Kategori).Include(u => u.Depo)
                .FirstOrDefaultAsync(m => m.Id == id &&
                (AktifSinifId.HasValue ? m.SinifId == AktifSinifId.Value : (m.UserId == userId && m.SinifId == null)));

            if (urun == null) return NotFound();

            ViewBag.SadeceOkuma = await SadeceOkumaYetkisiVarMi();
            return View(urun);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var urun = await _context.Urunler.Include(u => u.Kategori).Include(u => u.Depo)
                .FirstOrDefaultAsync(m => m.Id == id &&
                (AktifSinifId.HasValue ? m.SinifId == AktifSinifId.Value : (m.UserId == userId && m.SinifId == null)));

            if (urun == null) return NotFound();
            return View(urun);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (AdminBireyselGirisYasakMi()) return RedirectToAction("Index", "Home");
            if (await SadeceOkumaYetkisiVarMi()) return RedirectToAction(nameof(Index));

            var userId = _userManager.GetUserId(User);
            var urun = await _context.Urunler.FirstOrDefaultAsync(u => u.Id == id &&
                (AktifSinifId.HasValue ? u.SinifId == AktifSinifId.Value : (u.UserId == userId && u.SinifId == null)));

            if (urun != null)
            {
                _context.Urunler.Remove(urun);
                await _context.SaveChangesAsync();
                TempData["Basari"] = "Ürün başarıyla silindi.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> BarkodKontrol(string barkod)
        {
            var userId = _userManager.GetUserId(User);
            var urun = await _context.Urunler.FirstOrDefaultAsync(u => u.Barkod == barkod &&
                (AktifSinifId.HasValue ? u.SinifId == AktifSinifId.Value : (u.UserId == userId && u.SinifId == null)));

            if (urun != null) return Json(new { sonuc = "var", id = urun.Id, ad = urun.Ad });
            return Json(new { sonuc = "yok" });
        }

        private bool UrunExists(int id)
        {
            var userId = _userManager.GetUserId(User);
            return _context.Urunler.Any(e => e.Id == id &&
                (AktifSinifId.HasValue ? e.SinifId == AktifSinifId.Value : (e.UserId == userId && e.SinifId == null)));
        }
    }
}