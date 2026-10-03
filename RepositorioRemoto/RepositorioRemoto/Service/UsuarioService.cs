using Refit;
using RepositorioRemoto.Api;
using RepositorioRemoto.Dto;
using RepositorioRemoto.Errors;
using RepositorioRemoto.Models;

namespace RepositorioRemoto.Service;

public abstract record Result<T, E> where E : DomainErrors {
    public sealed record Success(T Value) : Result<T, E>;

    public sealed record Failure(E Error) : Result<T, E>;

    public static Result<T, E> Ok(T value) => new Success(value);
    public static Result<T, E> Fail(E error) => new Failure(error);

    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;
}

public record UsuarioService(IJsonPlaceholderApi api) {
    public async Task<List<Usuario>> GetAllAsync() {
        return await api.GetUsuariosAsync();
    }

    public async Task<Result<Usuario, DomainErrors>> GetByIdAsync(int id) {
        try {
            var usuario = await api.GetUsuarioByIdAsync(id);
            return usuario is not null
                ? Result<Usuario, DomainErrors>.Ok(usuario)
                : Result<Usuario, DomainErrors>.Fail(
                    new DomainErrors.NotFound("Usuario", id));
        }
        catch (ApiException ex) {
            return Result<Usuario, DomainErrors>.Fail(
                new DomainErrors.ApiError((int)ex.StatusCode, ex.Message));
        }
    }

    public async Task<Result<Usuario, DomainErrors>> CreateAsync(CreateUserRequest request) {
        if (string.IsNullOrWhiteSpace(request.Name)) {
            return Result<Usuario, DomainErrors>.Fail(
                new DomainErrors.ValidationError("Name", "El nombre es obligatorio"));
        }
        if (string.IsNullOrWhiteSpace(request.Username)) {
            return Result<Usuario, DomainErrors>.Fail(
                new DomainErrors.ValidationError("Username", "El nick es obligatorio"));
        }
        if (string.IsNullOrWhiteSpace(request.Email)) {
            return Result<Usuario, DomainErrors>.Fail(
                new DomainErrors.ValidationError("Email", "El mail es obligatorio"));
        }

        try {
            var creado = await api.CreateUsuarioAsync(request);
            return Result<Usuario, DomainErrors>.Ok(creado);
        }
        catch (ApiException ex) {
            return Result<Usuario, DomainErrors>.Fail(
                new DomainErrors.ApiError((int)ex.StatusCode, ex.Message));
        }
    }

    public async Task<Result<Usuario, DomainErrors>> UpdateAsync(int id, UpdateUserRequest request) {
        try {
            var actualizado = await api.UpdateUsuarioAsync(id, request);
            return Result<Usuario, DomainErrors>.Ok(actualizado);
        }
        catch (ApiException ex) {
            return Result<Usuario, DomainErrors>.Fail(
                new DomainErrors.NotFound("Usuario", id));
        }
    }

    public async Task<Result<bool, DomainErrors>> DeleteAsync(int id) {
        try {
            await api.DeleteUsuarioAsync(id);
            return Result<bool, DomainErrors>.Ok(true);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
            return Result<bool, DomainErrors>.Fail(
                new DomainErrors.NotFound("Usuario", id));
        }
        
    }
};