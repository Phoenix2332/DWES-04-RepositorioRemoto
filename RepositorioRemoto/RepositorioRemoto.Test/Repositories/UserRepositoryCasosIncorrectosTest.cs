using FluentAssertions;
using RepositorioRemoto.Errors;

namespace RepositorioRemoto.Test.Repositories;

[TestFixture]
public class UserRepositoryCasosIncorrectosTest : UserRepositoryTestBase {
    [Test]
    public async Task GetByIdAsync_UsuarioInexistente_RetornaNotFound() {
        // Arrange

        // Act
        var result = await Repository.GetByIdAsync(99);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserErrors.NotFoundError>();
    }

    [Test]
    public async Task UpdateAsync_UsuarioInexistente_RetornaNotFound() {
        // Arrange
        var user = CreateUser(99);

        // Act
        var result = await Repository.UpdateAsync(99, user);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserErrors.NotFoundError>();
    }

    [Test]
    public async Task DeleteAsync_UsuarioInexistente_RetornaNotFound() {
        // Arrange

        // Act
        var result = await Repository.DeleteAsync(99);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserErrors.NotFoundError>();
    }

    [Test]
    public async Task CreateAsync_ContextoDisposed_RetornaErrorDeCreacion() {
        // Arrange
        Context.Dispose();
        var user = CreateUser(1);

        // Act
        var result = await Repository.CreateAsync(user);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<RepositoryErrors.CreationError>();
    }

    [Test]
    public async Task DeleteAllAsync_ContextoDisposed_RetornaErrorDeBorrado() {
        // Arrange
        Context.Dispose();

        // Act
        var result = await Repository.DeleteAllAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<RepositoryErrors.DeletedError>();
    }
}
