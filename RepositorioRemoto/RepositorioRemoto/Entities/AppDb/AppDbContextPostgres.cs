using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RepositorioRemoto.Models;
namespace RepositorioRemoto.Entities.AppDb;

public class AppDbContextPostgre(DbContextOptions<AppDbContextPostgre> options) : DbContext(options), IAppDbContext {
    private static readonly JsonSerializerOptions _jsonOptions = new();

    private static readonly ValueConverter<Address, string> _addressToJson = new(
        address => JsonSerializer.Serialize(address, _jsonOptions),
        json => JsonSerializer.Deserialize<Address>(json, _jsonOptions)!);

    private static readonly ValueComparer<Address> _addressJsonComparer = new(
        (a, b) => JsonSerializer.Serialize(a, _jsonOptions) == JsonSerializer.Serialize(b, _jsonOptions),
        a => JsonSerializer.Serialize(a, _jsonOptions).GetHashCode(),
        a => JsonSerializer.Deserialize<Address>(JsonSerializer.Serialize(a, _jsonOptions), _jsonOptions)!);

    private static readonly ValueConverter<Company, string> _companyToJson = new(
        company => JsonSerializer.Serialize(company, _jsonOptions),
        json => JsonSerializer.Deserialize<Company>(json, _jsonOptions)!);

    private static readonly ValueComparer<Company> _companyJsonComparer = new(
        (a, b) => JsonSerializer.Serialize(a, _jsonOptions) == JsonSerializer.Serialize(b, _jsonOptions),
        a => JsonSerializer.Serialize(a, _jsonOptions).GetHashCode(),
        a => JsonSerializer.Deserialize<Company>(JsonSerializer.Serialize(a, _jsonOptions), _jsonOptions)!);

    public DbSet<User> Users => Set<User>();

    public void EnsureCreated() {
        Database.EnsureCreated();
    }

    public EntityEntry<User> Entry(User entity) => base.Entry(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>(e => {
            e.ToTable("users");
            e.HasKey(x => x.Id);

            e.Property(x => x.Name)
                .HasMaxLength(150);

            e.Property(x => x.UserName)
                .HasMaxLength(200);

            e.Property(x => x.Email)
                .HasMaxLength(150);

            e.Property(x => x.Address)
                .HasConversion(_addressToJson)
                .HasColumnName("address")
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(_addressJsonComparer);

            e.Property(x => x.Phone)
                .IsRequired()
                .HasMaxLength(50);

            e.Property(x => x.Website)
                .HasMaxLength(300);

            e.Property(x => x.Company)
                .HasConversion(_companyToJson)
                .HasColumnName("company")
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(_companyJsonComparer);
        });
    }
}