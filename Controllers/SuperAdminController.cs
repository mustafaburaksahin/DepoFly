using DepoFly.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DepoFly.Data;

namespace DepoFly.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController(AppDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly AppDbContext _context = context;
        //UserManager tipini ApplicationUser yaparak sistemi eşitledik.
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        // 1. DASHBOARD: Genel istatistikleri toplar
        public async Task<IActionResult> Index()
        {
            ViewBag.ToplamKullanici = await _userManager.Users.CountAsync();
            ViewBag.ToplamSinif = await _context.Siniflar.CountAsync();
            ViewBag.AktifSinif = await _context.Siniflar.CountAsync(s => s.AktifMi && !s.SistemdenBanliMi);
            ViewBag.ToplamUrun = await _context.Urunler.CountAsync();

            return View();
        }

        // 2. KULLANICI LİSTESİ
        public async Task<IActionResult> Kullanicilar()
        {
            // Artık ApplicationUser listesi mermi gibi gelecek
            var kullanicilar = await _userManager.Users.ToListAsync();
            return View(kullanicilar);
        }

        // 3. SINIF LİSTESİ
        public async Task<IActionResult> Siniflar()
        {
            // ThenInclude(sk => sk.User) artık ApplicationUser tipindeki kullanıcıyı çeker
            var siniflar = await _context.Siniflar
                .Include(s => s.SinifKullanicilari)
                    .ThenInclude(sk => sk.User)
                .ToListAsync();

            return View(siniflar);
        }

        // --- AKSİYONLAR ---

        // ADMIN YETKİSİ VER/AL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminlikYetkisiVer(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Admin");
                    TempData["Bilgi"] = "Adminlik yetkisi geri alındı.";
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                    TempData["Basari"] = "Kullanıcı başarıyla Admin yapıldı.";
                }
            }
            return RedirectToAction("Kullanicilar");
        }

        // KULLANICIYI BANLA (Identity Lockout)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KullaniciBanla(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var isLocked = await _userManager.IsLockedOutAsync(user);
                // Eğer banlıysa aç, değilse MaxValue ile 100 yıl kilitle
                await _userManager.SetLockoutEndDateAsync(user, isLocked ? null : DateTimeOffset.MaxValue);

                TempData["Basari"] = isLocked ? "Kullanıcının banı kaldırıldı." : "Kullanıcı sistemden banlandı.";
            }
            return RedirectToAction("Kullanicilar");
        }

        // SINIFI ERİŞİME KAPAT/AÇ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SinifDurumDegistir(int id)
        {
            var sinif = await _context.Siniflar.FindAsync(id);
            if (sinif != null)
            {
                sinif.SistemdenBanliMi = !sinif.SistemdenBanliMi;
                await _context.SaveChangesAsync();
                TempData["Basari"] = sinif.SistemdenBanliMi ? "Sınıf erişime kapatıldı." : "Sınıf erişime açıldı.";
            }
            return RedirectToAction("Siniflar");
        }

        // SINIFI VE BAĞLI VERİLERİ SİL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SinifSil(int id)
        {
            var sinif = await _context.Siniflar
                .Include(s => s.SinifKullanicilari)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sinif != null)
            {
                // Önce sınıfa bağlı üyelikleri temizliyoruz
                _context.SinifKullanicilari.RemoveRange(sinif.SinifKullanicilari);

                // Sonra sınıfın kendisini siliyoruz
                _context.Siniflar.Remove(sinif);
                await _context.SaveChangesAsync();
                TempData["Basari"] = "Sınıf ve bağlı tüm veriler sistemden silindi.";
            }
            return RedirectToAction("Siniflar");
        }
    }
}