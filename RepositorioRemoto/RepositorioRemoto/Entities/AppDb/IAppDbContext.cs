using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RepositorioRemoto.Models;

namespace RepositorioRemoto.Entities.AppDb;

public interface IAppDbContext {
    DbSet<User> Users { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    void EnsureCreated();

    EntityEntry<User> Entry(User entity);
}