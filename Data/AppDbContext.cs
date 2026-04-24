using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DepoFly.Models;
using Microsoft.AspNetCore.Identity;

namespace DepoFly.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Urun> Urunler { get; set; }
        public DbSet<Kategori> Kategori { get; set; }
        public DbSet<Depo> Depolar { get; set; }
        public DbSet<Not> Not { get; set; }

        // --- SINIF VE YETKİLENDİRME SİSTEMİ ---
        public DbSet<Sinif> Siniflar { get; set; }
        public DbSet<SinifKullanici> SinifKullanicilari { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            //base call mutlaka en üstte kalmalı. 
            // Identity'nin kendi içindeki tablo yapılandırmalarını (AspNetUsers vb.) bu kurar.
            base.OnModelCreating(builder);

            // 1. SinifKullanici için Composite Key (SinifId + UserId)
            builder.Entity<SinifKullanici>()
                .HasKey(sk => new { sk.SinifId, sk.UserId });

            // 2. Sınıf ile SinifKullanici İlişkisi
            builder.Entity<SinifKullanici>()
                .HasOne(sk => sk.Sinif)
                .WithMany(s => s.SinifKullanicilari)
                .HasForeignKey(sk => sk.SinifId);

            // 3. ApplicationUser ile SinifKullanici İlişkisi
            //KRİTİK: İlişkiyi artık tamamen ApplicationUser (sk.User) üzerinden yürütüyoruz.
            builder.Entity<SinifKullanici>()
                .HasOne(sk => sk.User)
                .WithMany()
                .HasForeignKey(sk => sk.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Kullanıcı silinirse sınıftan da silinsin.
        }
    }
}