using Microsoft.EntityFrameworkCore;

namespace glint_backend.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }
        public DbSet<Models.User> Users { get; set; } = null!;
        public DbSet<Models.Resume> Resumes { get; set; } = null!;
        public DbSet<Models.JobAdvertisement> JobAdvertisements { get; set; } = null!;
        public DbSet<Models.Analysis> Analyses { get; set; } = null!;
        public DbSet<Models.AnalysisResult> AnalysisResults { get; set; } = null!;
        public DbSet<Models.OneTimeCode> OneTimeCodes { get; set; } = null!;
        public DbSet<Models.RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Models.User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Models.Analysis>()
                .HasOne(a => a.Resume)
                .WithMany(r => r.Analyses)
                .HasForeignKey(a => a.ResumeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.Analysis>()
                .HasOne(a => a.JobAdvertisement)
                .WithMany(j => j.Analyses)
                .HasForeignKey(a => a.JobAdvertisementId)
                .OnDelete(DeleteBehavior.NoAction);
        }

    }
}
