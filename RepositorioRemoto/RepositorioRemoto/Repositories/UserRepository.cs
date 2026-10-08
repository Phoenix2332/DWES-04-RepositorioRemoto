using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using RepositorioRemoto.Entities.AppDb;
using RepositorioRemoto.Errors;
using RepositorioRemoto.Models;
using Serilog;

namespace RepositorioRemoto.Repositories;

public class UserRepository : IUserRepository {
    private static readonly ILogger _logger = Log.ForContext<UserRepository>();
    private readonly IAppDbContext _context;

    public UserRepository(IAppDbContext context) {
        _context = context;
        _logger.Debug("[REPO-INIT] Comprobando base de datos");
        _context.Database.EnsureCreated();
        _logger.Debug("[REPO-INIT] Base de datos inicializada correctamente");
    }

    public async Task<Result<User, DomainErrors>> GetByIdAsync(int id) {
        _logger.Debug("[REPO-GET-BY-ID] Obteniendo usuario con ID {Id}", id);
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null) {
            _logger.Debug("[REPO-GET-BY-ID] No se ha encontrado el usuario con ID {Id}", id);

            return Result.Failure<User, DomainErrors>(UsersError.NotFoundError(id));
        }

        _logger.Debug("[REPO-GET-BY-ID] Usuario con ID {Id} obtenido correctamente", id);
        return Result.Success<User, DomainErrors>(user);
    }

    public async Task<List<User>> GetAllAsync() {
        _logger.Debug("[REPO-GET-ALL] Obteniendo todos los usuarios");
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.Id)
            .ToListAsync();

        _logger.Debug("[REPO-GET-ALL] Se han obtenido {Count} usuarios", users.Count);
        return users;
    }

    public async Task<Result<User, DomainErrors>> CreateAsync(User user) {
        _logger.Debug("[REPO-CREATE] Creando usuario con ID {Id}", user.Id);
        try {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _logger.Debug("[REPO-CREATE] Usuario con ID {Id} creado correctamente", user.Id);
            return Result.Success<User, DomainErrors>(user);
        }
        catch (Exception ex) {
            _logger.Error(ex, "[REPO-CREATE] Error al crear usuario con ID {Id}. Mensaje: {Message}", user.Id,
                ex.Message);
            return Result.Failure<User, DomainErrors>(RepositoryError.CreationError());
        }
    }

    public async Task<Result<User, DomainErrors>> UpdateAsync(int id, User user) {
        _logger.Debug("[REPO-UPDATE] Actualizando usuario con ID {Id}", id);
        try {
            var existing = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (existing is null) {
                _logger.Debug("[REPO-UPDATE] No se ha encontrado el usuario con ID {Id}", id);
                return Result.Failure<User, DomainErrors>(UsersError.NotFoundError(id));
            }

            // Detach de la instancia rastreada: quien llama (p. ej. UserService tras
            // ComprobarExistenciaAsync) puede tenerla ya en el mismo contexto y
            // Update(otraInstancia) lanzaría "already being tracked".
            _context.Entry(existing).State = EntityState.Detached;
            _context.Users.Update(user);

            await _context.SaveChangesAsync();

            _logger.Debug("[REPO-UPDATE] Usuario con ID {Id} actualizado correctamente", id);
            return Result.Success<User, DomainErrors>(user);
        }
        catch (Exception ex) {
            _logger.Error(ex, "[REPO-UPDATE] Error al actualizar usuario con ID {Id}", id);
            return Result.Failure<User, DomainErrors>(RepositoryError.UpdatedError());
        }
    }

    public async Task<Result<User, DomainErrors>> DeleteAsync(int id) {
        _logger.Debug("[REPO-DELETE] Eliminando usuario con ID {Id}", id);
        try {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user is null) {
                _logger.Debug("[REPO-DELETE] No se ha encontrado el usuario con ID {Id}", id);
                return Result.Failure<User, DomainErrors>(UsersError.NotFoundError(id));
            }

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            _logger.Debug("[REPO-DELETE] Usuario con ID {Id} eliminado correctamente", id);
            return Result.Success<User, DomainErrors>(user);
        }
        catch (Exception ex) {
            _logger.Error(ex, "[REPO-DELETE] Error al eliminar usuario con ID {Id}", id);
            return Result.Failure<User, DomainErrors>(RepositoryError.DeletedError());
        }
    }

    public async Task<Result<bool, DomainErrors>> DeleteAllAsync() {
        _logger.Debug("[REPO-DELETE-ALL] Eliminando todos los usuarios");
        try {
            await _context.Users.ExecuteDeleteAsync();

            _logger.Debug("[REPO-DELETE-ALL] Todos los usuarios han sido eliminados correctamente");
            return Result.Success<bool, DomainErrors>(true);
        }
        catch (Exception ex) {
            _logger.Error(ex, "[REPO-DELETE-ALL] Error al eliminar todos los usuarios");
            return Result.Failure<bool, DomainErrors>(RepositoryError.DeletedError());
        }
    }
}