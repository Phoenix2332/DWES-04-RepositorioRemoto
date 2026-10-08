using System.Net;
using CSharpFunctionalExtensions;
using Refit;
using RepositorioRemoto.Api;
using RepositorioRemoto.Cache.Common;
using RepositorioRemoto.Config;
using RepositorioRemoto.Dto;
using RepositorioRemoto.Enums;
using RepositorioRemoto.Errors;
using RepositorioRemoto.Mappers;
using RepositorioRemoto.Models;
using RepositorioRemoto.Repositories;
using RepositorioRemoto.Service.Notifications;
using RepositorioRemoto.Storages;
using RepositorioRemoto.Validators;
using Serilog;
using Notification = RepositorioRemoto.Models.Notification;

namespace RepositorioRemoto.Service.Users;

public class UserService(
    IUserValidator validator,
    IUserRepository repository,
    IUserCache cache,
    IUserStorage storage,
    INotificationService notificationService,
    IJsonPlaceholderApi api
) : IUserService {
    private static readonly ILogger _logger = Log.ForContext<UserService>();

    public async Task<Result<IEnumerable<User>, DomainErrors>> GetAllAsync() {
        _logger.Debug("[SERVICE-GET-ALL] Obteniendo todos los usuarios");
        try {
            var locales = await repository.GetAllAsync();

            if (locales.Count != 0) {
                _logger.Debug("[SERVICE-GET-ALL] Se han obtenido {Count} usuarios de la base de datos", locales.Count);
                return Result.Success<IEnumerable<User>, DomainErrors>(locales);
            }

            _logger.Debug("[SERVICE-GET-ALL] No hay usuarios locales. Obteniendo usuarios desde la API");
            var remotos = await api.GetUsuariosAsync();

            _logger.Debug("[SERVICE-GET-ALL] Se han obtenido {Count} usuarios desde la API", remotos.Count);
            foreach (var user in remotos)
                await repository.CreateAsync(user);

            _logger.Debug("[SERVICE-GET-ALL] Se han guardado {Count} usuarios en la base de datos", remotos.Count);
            return Result.Success<IEnumerable<User>, DomainErrors>(remotos);
        }
        catch (ApiException ex) {
            _logger.Error(ex, "[SERVICE-GET-ALL] Error al obtener los usuarios de la API");
            return Result.Failure<IEnumerable<User>, DomainErrors>(
                new ApiErrors("Error al obtener los usuarios de la API."));
        }
        catch (Exception ex) {
            _logger.Error(ex, "[SERVICE-GET-ALL] Error al obtener los usuarios");
            return Result.Failure<IEnumerable<User>, DomainErrors>(ServiceError.GetAllError());
        }
    }

    public async Task<Result<User, DomainErrors>> GetByIdAsync(int id) {
        _logger.Debug("[SERVICE-GET-BY-ID] Obteniendo usuario con ID {Id}", id);
        try {
            var cacheado = await cache.GetAsync(id);

            if (cacheado is not null) {
                _logger.Debug("[SERVICE-GET-BY-ID] Usuario con ID {Id} encontrado en caché", id);
                return Result.Success<User, DomainErrors>(cacheado);
            }

            _logger.Debug("[SERVICE-GET-BY-ID] Usuario con ID {Id} no encontrado en caché", id);
            var local = await repository.GetByIdAsync(id);

            if (local.IsSuccess) {
                _logger.Debug("[SERVICE-GET-BY-ID] Usuario con ID {Id} encontrado en base de datos", id);
                await cache.AddAsync(local.Value);

                return Result.Success<User, DomainErrors>(local.Value);
            }

            _logger.Debug("[SERVICE-GET-BY-ID] Usuario con ID {Id} no encontrado en base de datos. Consultando API",
                id);
            var remoto = await api.GetUsuarioByIdAsync(id);

            if (remoto is null) {
                _logger.Debug("[SERVICE-GET-BY-ID] Usuario con ID {Id} no encontrado en la API", id);
                return Result.Failure<User, DomainErrors>(UsersError.NotFoundError(id));
            }

            var guardado = await repository.CreateAsync(remoto);

            if (guardado.IsFailure) {
                _logger.Error("[SERVICE-GET-BY-ID] Error al guardar el usuario con ID {Id}", id);
                return Result.Failure<User, DomainErrors>(guardado.Error);
            }

            await cache.AddAsync(guardado.Value);

            _logger.Debug("[SERVICE-GET-BY-ID] Usuario con ID {Id} obtenido de la API y guardado correctamente", id);
            return Result.Success<User, DomainErrors>(guardado.Value);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            _logger.Debug(ex, "[SERVICE-GET-BY-ID] Usuario con ID {Id} no encontrado en la API", id);
            return Result.Failure<User, DomainErrors>(UsersError.NotFoundError(id));
        }
        catch (Exception ex) {
            _logger.Error(ex, "[SERVICE-GET-BY-ID] Error al obtener usuario con ID {Id}", id);
            return Result.Failure<User, DomainErrors>(ServiceError.GetByIdError(id));
        }
    }

    public async Task<Result<User, DomainErrors>> CreateAsync(CreateUserRequest request) {
        _logger.Debug("[SERVICE-CREATE] Creando usuario");
        try {
            var usuario = request.ToModel();

            return await validator.Validate(usuario)
                .Tap(_ => _logger.Debug("[SERVICE-CREATE] Usuario validado correctamente"))
                .Map(_ => api.CreateUsuarioAsync(request))
                .Tap(creado => _logger.Debug("[SERVICE-CREATE] Usuario creado en API con ID {Id}", creado.Id))
                .Map(creado => usuario with { Id = creado.Id })
                .Bind(repository.CreateAsync)
                .Tap(user => {
                    _logger.Debug("[SERVICE-CREATE] Usuario con ID {Id} guardado correctamente", user.Id);
                    notificationService.Notificar(
                        new Notification(
                            TypeNotification.Create,
                            $"Se ha creado el usuario con ID {user.Id}.",
                            DateTime.UtcNow));
                });
        }
        catch (ApiException ex) {
            _logger.Error(ex, "[SERVICE-CREATE] Error al crear el usuario en la API");
            return Result.Failure<User, DomainErrors>(new ApiErrors("Error al crear el usuario en la API."));
        }
        catch (Exception ex) {
            _logger.Error(ex, "[SERVICE-CREATE] Error al crear el usuario");
            return Result.Failure<User, DomainErrors>(ServiceError.CreateError());
        }
    }

    public async Task<Result<User, DomainErrors>> UpdateAsync(int id, UpdateUserRequest request) {
        _logger.Debug("[SERVICE-UPDATE] Actualizando usuario con ID {Id}", id);
        try {
            return await ComprobarExistenciaAsync(id)
                .Tap(_ => _logger.Debug("[SERVICE-UPDATE] Usuario con ID {Id} encontrado", id))
                .Tap(_ => api.UpdateUsuarioAsync(id, request))
                .Tap(_ => _logger.Debug("[SERVICE-UPDATE] Usuario con ID {Id} actualizado en API", id))
                .Bind(_ =>
                    repository.UpdateAsync(
                        id,
                        request.ToModel()))
                .Tap(_ => {
                    _logger.Debug("[SERVICE-UPDATE] Usuario con ID {Id} actualizado en base de datos", id);

                    cache.RemoveAsync(id);
                })
                .Tap(_ => {
                    _logger.Debug("[SERVICE-UPDATE] Caché del usuario con ID {Id} eliminada", id);

                    notificationService.Notificar(
                        new Notification(
                            TypeNotification.Update,
                            $"Se ha actualizado el usuario con ID {id}.",
                            DateTime.UtcNow));
                });
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            _logger.Debug(ex, "[SERVICE-UPDATE] Usuario con ID {Id} no encontrado en la API", id);
            return Result.Failure<User, DomainErrors>(UsersError.NotFoundError(id));
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.InternalServerError) {
            // JSONPlaceholder devuelve 500 (no 404) al hacer PUT sobre un ID
            // inexistente en remoto (p. ej. el 11 falsificado por su POST).
            _logger.Debug(ex, "[SERVICE-UPDATE] Usuario con ID {Id} no encontrado en la API (500)", id);
            return Result.Failure<User, DomainErrors>(UsersError.NotFoundError(id));
        }
        catch (Exception ex) {
            _logger.Error(ex, "[SERVICE-UPDATE] Error al actualizar usuario con ID {Id}", id);
            return Result.Failure<User, DomainErrors>(ServiceError.UpdateError(id));
        }
    }

    public async Task<Result<User, DomainErrors>> DeleteAsync(int id) {
        _logger.Debug("[SERVICE-DELETE] Eliminando usuario con ID {Id}", id);
        try {
            return await ComprobarExistenciaAsync(id)
                .Tap(_ => _logger.Debug("[SERVICE-DELETE] Usuario con ID {Id} encontrado", id))
                .Tap(_ => api.DeleteUsuarioAsync(id))
                .Tap(_ => _logger.Debug("[SERVICE-DELETE] Usuario con ID {Id} eliminado de la API", id))
                .Bind(_ => repository.DeleteAsync(id))
                .Tap(_ => {
                    _logger.Debug("[SERVICE-DELETE] Usuario con ID {Id} eliminado de la base de datos", id);

                    cache.RemoveAsync(id);
                })
                .Tap(_ => {
                    _logger.Debug("[SERVICE-DELETE] Caché del usuario con ID {Id} eliminada", id);

                    notificationService.Notificar(
                        new Notification(
                            TypeNotification.Delete,
                            $"Se ha eliminado el usuario con ID {id}.",
                            DateTime.UtcNow));
                });
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            _logger.Debug(ex, "[SERVICE-DELETE] Usuario con ID {Id} no encontrado en la API", id);
            return Result.Failure<User, DomainErrors>(UsersError.NotFoundError(id));
        }
        catch (Exception ex) {
            _logger.Error(ex, "[SERVICE-DELETE] Error al eliminar usuario con ID {Id}", id);
            return Result.Failure<User, DomainErrors>(ServiceError.DeleteError(id));
        }
    }

    public async Task<Result<bool, DomainErrors>> ExportToJsonAsync() {
        _logger.Debug("[SERVICE-EXPORT] Exportando usuarios a JSON");
        try {
            var items = await repository.GetAllAsync();

            _logger.Debug("[SERVICE-EXPORT] Se han obtenido {Count} usuarios para exportar", items.Count);
            var result = await storage.ExportarJsonAsync(items.AsEnumerable(), AppConfig.UsersJsonPath);

            if (result.IsFailure) {
                _logger.Error("[SERVICE-EXPORT] Error al exportar usuarios: {Message}", result.Error.Message);

                return Result.Failure<bool, DomainErrors>(ServiceError.ExportToJsonError(result.Error.Message));
            }

            _logger.Debug("[SERVICE-EXPORT] Usuarios exportados correctamente a {Path}", AppConfig.UsersJsonPath);
            return Result.Success<bool, DomainErrors>(true);
        }
        catch (Exception ex) {
            _logger.Error(ex, "[SERVICE-EXPORT] Error al realizar la exportación");
            return Result.Failure<bool, DomainErrors>(
                ServiceError.ExportToJsonError("No se ha podido realizar la exportación."));
        }
    }

    private async Task<Result<User, DomainErrors>> ComprobarExistenciaAsync(int id) {
        _logger.Debug("[SERVICE-CHECK-EXISTENCE] Comprobando existencia del usuario con ID {Id}", id);
        var result = await repository.GetByIdAsync(id);

        return result.IsSuccess
            ? Result.Success<User, DomainErrors>(result.Value)
            : Result.Failure<User, DomainErrors>(UsersError.NotFoundError(id));
    }
}