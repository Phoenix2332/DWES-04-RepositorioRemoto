using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace RepositorioRemoto.Test.Repositories;

[TestFixture]
public class UserRepositoryCasosCorrectosTest : UserRepositoryTestBase {
    [Test]
    public async Task GetByIdAsync_UsuarioExistente_RetornaUsuario() {
        // Arrange
        var user = CreateUser(1);
        await AddUserAsync(user);

        // Act
        var result = await Repository.GetByIdAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(user);
    }

    [Test]
    public async Task GetAllAsync_ConUsuarios_RetornaOrdenadosPorId() {
        // Arrange
        await AddUserAsync(CreateUser(2));
        await AddUserAsync(CreateUser(1));

        // Act
        var result = await Repository.GetAllAsync();

        // Assert
        result.Select(user => user.Id).Should().Equal(1, 2);
    }

    [Test]
    public async Task CreateAsync_UsuarioValido_PersisteYRetornaUsuario() {
        // Arrange
        var user = CreateUser(1);

        // Act
        var result = await Repository.CreateAsync(user);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Context.Users.FindAsync(1)).Should().BeEquivalentTo(user);
    }

    [Test]
    public async Task UpdateAsync_UsuarioExistente_RetornaUsuarioActualizado() {
        // Arrange
        await AddUserAsync(CreateUser(1));
        var updated = await Context.Users.SingleAsync(user => user.Id == 1);

        // Act
        var result = await Repository.UpdateAsync(1, updated);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(updated);
    }

    [Test]
    public async Task UpdateAsync_EntidadSeparada_ActualizaCorrectamente() {
        // Arrange: instancia detached con datos modificados (caso real DTO).
        await AddUserAsync(CreateUser(1));
        Context.ChangeTracker.Clear();
        var detachedUser = CreateUser(1) with { Name = "Updated" };

        // Act
        var result = await Repository.UpdateAsync(1, detachedUser);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Updated");
        (await Context.Users.FindAsync(1))!.Name.Should().Be("Updated");
    }

    [Test]
    public async Task DeleteAsync_UsuarioExistente_EliminaYRetornaUsuario() {
        // Arrange
        var user = CreateUser(1);
        await AddUserAsync(user);

        // Act
        var result = await Repository.DeleteAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Context.Users.FindAsync(1)).Should().BeNull();
    }

    [Test]
    public async Task DeleteAllAsync_ConUsuarios_EliminaTodosYRetornaTrue() {
        // Arrange
        await AddUserAsync(CreateUser(1));
        await AddUserAsync(CreateUser(2));

        // Act
        var result = await Repository.DeleteAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        (await Repository.GetAllAsync()).Should().BeEmpty();
    }
}
