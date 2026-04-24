using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using DepoFly.Services;
using DepoFly.Data;
using DepoFly.Models; // ApplicationUser modelini görmesi için ekledik
using System.Net;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Veritabanı Bağlantısı
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // 2. Identity & Güvenlik Ayarları (ApplicationUser'a GÜNCELLENDİ)
        builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.SignIn.RequireConfirmedAccount = false;

            // Boşluk ve Türkçe Karakter İzni
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ " +
                "ğüşıöçĞÜŞİÖÇ";

            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>();

        // 3. Dış Servisler (Google ve Facebook ile Giriş)
        builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "ID_YOK";
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "SECRET_YOK";
            })
            .AddFacebook(options =>
            {
                options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "APP_ID_YOK";
                options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "SECRET_YOK";
            });

        // 4. Servis Kayıtları
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();
        builder.Services.AddTransient<IEmailSender, EmailSender>();

        // Hafıza (Cache) Servisi
        builder.Services.AddMemoryCache();

        // Session (Oturum) Servisi
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(60);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        // Cookie Ayarları
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = $"/Identity/Account/Login";
            options.LogoutPath = $"/Identity/Account/Logout";
            options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
        });

        var app = builder.Build();

        // 5. Middleware (Ara Yazılım) Hattı
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseSession();

        app.UseAuthentication();
        app.UseAuthorization();

        // 6. Sayfa Eşleştirmeleri
        app.MapRazorPages();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        // --- SUPERADMIN VE ROL TANIMLAMA (ApplicationUser'a GÜNCELLENDİ) ---
        using (var scope = app.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); // Burası güncellendi

            // Global rolleri oluştur
            string[] roller = ["SuperAdmin", "Admin", "Kullanici"];
            foreach (var rol in roller)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                {
                    await roleManager.CreateAsync(new IdentityRole(rol));
                }
            }

            // DİKKAT: Buraya kendi mailini yaz.
            var superAdminMail = "mustafabu7780@gmail.com";

            var adminUser = await userManager.FindByEmailAsync(superAdminMail);
            if (adminUser != null)
            {
                if (!await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                }
            }
        }

        app.Run();
    }
}