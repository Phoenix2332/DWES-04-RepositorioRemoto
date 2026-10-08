using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RepositorioRemoto.Models;

namespace RepositorioRemoto.Entities.AppDb;

public class AppDbContextSqlite(DbContextOptions<AppDbContextSqlite> options) : DbContext(options), IAppDbContext {
    public DbSet<User> Users => Set<User>();

    public void EnsureCreated() {
        Database.EnsureCreated();
    }

    public EntityEntry<User> Entry(User entity) => base.Entry(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>(entity => {
            entity.ToTable("tbl_user");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.UserName)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Phone)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Website)
                .IsRequired()
                .HasMaxLength(250);

            entity.OwnsOne(e => e.Address, address => {
                address.ToJson();
                address.OwnsOne(e => e.Geo);
            });

            entity.OwnsOne(e => e.Company, company => { company.ToJson(); });
        });
    }
}