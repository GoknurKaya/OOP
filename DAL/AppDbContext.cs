using Microsoft.EntityFrameworkCore;
using DAL.Models.General;

namespace DAL
{
    public class AppDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; }

        // Parametresiz constructor → Migrations için şart
        public AppDbContext() { }

        // DI kullanacaksan bu da dursun (opsiyonel)
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    "Server=GÖKNUR\\SQLEXPRESS;Database=MyAppDB;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>().ToTable("Players");
        }
    }
}
