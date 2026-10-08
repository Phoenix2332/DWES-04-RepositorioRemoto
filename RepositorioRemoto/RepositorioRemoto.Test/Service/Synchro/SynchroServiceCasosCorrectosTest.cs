using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RepositorioRemoto.Api;
using RepositorioRemoto.Cache.Common;
using RepositorioRemoto.Config;
using RepositorioRemoto.Errors;
using RepositorioRemoto.Models;
using RepositorioRemoto.Repositories;
using RepositorioRemoto.Service.Synchro;

namespace RepositorioRemoto.Test.Service.Synchro;

[TestFixture]
public class SynchroServiceCasosCorrectosTest {
    [Test]
    public async Task SynchronizeOnceAsync_UsuariosRemotos_LimpiaYGuardaUsuarios() {
        // Arrange
        var cache = new Mock<IUserCache>();
        var repository = new Mock<IUserRepository>();
        var api = new Mock<IJsonPlaceholderApi>();
        var users = new List<User> { CreateUser(1), CreateUser(2) };
        repository.Setup(x => x.DeleteAllAsync()).ReturnsAsync(Result.Success<bool, DomainErrors>(true));
        repository.Setup(x => x.CreateAsync(It.IsAny<User>())).ReturnsAsync((User user) => Result.Success<User, DomainErrors>(user));
        api.Setup(x => x.GetUsuariosAsync()).ReturnsAsync(users);
        using var provider = CreateProvider(repository, api);
        var service = new SynchroService(provider, cache.Object);

        // Act
        await service.SynchronizeOnceAsync();

        // Assert
        cache.Verify(x => x.ClearAsync(), Times.Once);
        repository.Verify(x => x.DeleteAllAsync(), Times.Once);
        repository.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Exactly(2));
        api.Verify(x => x.GetUsuariosAsync(), Times.Once);
    }

    [Test]
    public async Task StartAsync_TokenYaCancelado_NoSincroniza()
    {
        // Arrange: token cancelado antes de arrancar; el PeriodicTimer
        // lanza OperationCanceledException sin ejecutar ninguna sincronización.
        AppConfig.Configure("dev");
        var cache = new Mock<IUserCache>();
        var repository = new Mock<IUserRepository>();
        var api = new Mock<IJsonPlaceholderApi>();
        using var provider = CreateProvider(repository, api);
        var service = new SynchroService(provider, cache.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var action = () => service.StartAsync(cts.Token);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
        cache.Verify(x => x.ClearAsync(), Times.Never);
        repository.Verify(x => x.DeleteAllAsync(), Times.Never);
        api.Verify(x => x.GetUsuariosAsync(), Times.Never);
    }

    private static ServiceProvider CreateProvider(Mock<IUserRepository> repository, Mock<IJsonPlaceholderApi> api) =>
        new ServiceCollection()
            .AddSingleton(repository.Object)
            .AddSingleton(api.Object)
            .BuildServiceProvider();

    private static User CreateUser(int id) => new(
        id, $"Name{id}", $"user{id}", $"user{id}@test.com",
        new Address("Street", "Suite", "City", "00000", new Geo("1", "2")),
        "555", "site", new Company("Company", "phrase", "bs"));
}
