using RepositorioRemoto.Dto;
using RepositorioRemoto.Models;

namespace RepositorioRemoto.Mappers;

public static class UsuarioMapper {

    public static CreateUserRequest ToCreateRequest(this Usuario usuario) => new(
        usuario.Name,
        usuario.Username,
        usuario.Email);

    public static UpdateUserRequest ToUpdateRequest(this Usuario usuario) => new(
        usuario.Id,
        usuario.Name,
        usuario.Username,
        usuario.Email);

    public static Usuario ToUsuario(this CreateUserRequest request, int id = 0) => new(
        id,
        request.Name,
        request.Username,
        request.Email);

    public static Usuario ToUsuario(this UpdateUserRequest request) => new(
        request.Id,
        request.Name,
        request.Username,
        request.Email);
}