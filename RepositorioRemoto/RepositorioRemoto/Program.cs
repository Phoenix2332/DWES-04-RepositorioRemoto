using Microsoft.Extensions.DependencyInjection;
using RepositorioRemoto.Api;
using RepositorioRemoto.Config;
using RepositorioRemoto.Infrastructure;
using RepositorioRemoto.Mappers;
using RepositorioRemoto.Models;
using RepositorioRemoto.Repositories;
using RepositorioRemoto.Service.Notifications;
using RepositorioRemoto.Service.Synchro;
using RepositorioRemoto.Service.Users;
using Serilog;
using static System.Console;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
    .CreateLogger();

WriteLine($"Número de argumentos: {args.Length}");

for (var i = 0; i < args.Length; i++)
    WriteLine($"args[{i}] = '{args[i]}'");

var profile = args.Length > 0 ? args[0].ToLower() : "dev";

WriteLine($"profile = '{profile}'");

AppConfig.Configure(profile);

WriteLine($"Perfil seleccionado: {AppConfig.Profile}");

var provider = InyectorDependencias.BuildServiceProvider();

using var scope = provider.CreateScope();

var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
var api = scope.ServiceProvider.GetRequiredService<IJsonPlaceholderApi>();
var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
var synchroService = scope.ServiceProvider.GetRequiredService<SynchroService>();

var cancellationTokenSource = new CancellationTokenSource();

CancelKeyPress += (_, e) => {
    e.Cancel = true;
    cancellationTokenSource.Cancel();
    WriteLine();
    WriteLine("Deteniendo aplicación...");
};

var subscription = notificationService.Observable.Subscribe(notification => {
    WriteLine($"[{notification.Date:HH:mm:ss}] " + $"{notification.Type}: " + $"{notification.Message}");
});

WriteLine();
WriteLine("Cargando usuarios desde JSONPlaceholder...");

await repository.DeleteAllAsync();

var users = await api.GetUsuariosAsync();

foreach (var user in users) await repository.CreateAsync(user);

WriteLine($"Se han cargado {users.Count} usuarios correctamente.");

WriteLine();
WriteLine("Servicio de sincronización iniciado.");

var syncTask = synchroService.StartAsync(cancellationTokenSource.Token);

await MenuAsync(userService, cancellationTokenSource);

await cancellationTokenSource.CancelAsync();

try {
    await syncTask;
}
catch (OperationCanceledException) {
}

subscription.Dispose();

WriteLine("Aplicación finalizada.");
return;

static async Task MenuAsync(IUserService userService, CancellationTokenSource cancellationTokenSource) {
    while (!cancellationTokenSource.IsCancellationRequested) {
        WriteLine();
        WriteLine("========================================");
        WriteLine("       REPOSITORIO REMOTO - USERS");
        WriteLine("========================================");
        WriteLine("1. Obtener todos los usuarios");
        WriteLine("2. Obtener usuario por ID");
        WriteLine("3. Crear usuario");
        WriteLine("4. Actualizar usuario");
        WriteLine("5. Eliminar usuario");
        WriteLine("6. Exportar usuarios a JSON");
        WriteLine("0. Salir");
        WriteLine("========================================");
        Write("Selecciona una opción: ");

        var option = ReadLine();

        WriteLine();

        switch (option) {
            case "1":
                await GetAllAsync(userService);
                break;

            case "2":
                await GetByIdAsync(userService);
                break;

            case "3":
                await CreateAsync(userService);
                break;

            case "4":
                await UpdateAsync(userService);
                break;

            case "5":
                await DeleteAsync(userService);
                break;

            case "6":
                await ExportAsync(userService);
                break;

            case "0":
                cancellationTokenSource.Cancel();
                WriteLine("Saliendo...");
                return;

            default:
                WriteLine("Opción no válida.");
                break;
        }

        if (!cancellationTokenSource.IsCancellationRequested) WriteLine();
    }
}

static async Task GetAllAsync(IUserService userService) {
    WriteLine("Obteniendo todos los usuarios...");

    var resultGet = await userService.GetAllAsync();

    if (resultGet.IsFailure) {
        WriteLine($"Error: {resultGet.Error.Message}");
        return;
    }

    var usersGet = resultGet.Value;

    WriteLine($"Se han obtenido {usersGet.Count()} usuarios.");
    WriteLine();

    foreach (var user in usersGet) WriteLine($"[{user.Id}] {user.Name} - {user.Email}");
}

static async Task GetByIdAsync(IUserService userService) {
    Write("Introduce el ID del usuario: ");

    if (!int.TryParse(ReadLine(), out var id)) {
        WriteLine("El ID no es válido.");
        return;
    }

    var resultGetBy = await userService.GetByIdAsync(id);

    if (resultGetBy.IsFailure) {
        WriteLine($"Error: {resultGetBy.Error.Message}");
        return;
    }

    var user = resultGetBy.Value;

    WriteLine("Usuario encontrado:");
    WriteLine($"ID:       {user.Id}");
    WriteLine($"Nombre:   {user.Name}");
    WriteLine($"Username: {user.UserName}");
    WriteLine($"Email:    {user.Email}");
    WriteLine($"Teléfono: {user.Phone}");
    WriteLine($"Web:      {user.Website}");
}

static async Task CreateAsync(IUserService userService) {
    WriteLine("=== CREAR USUARIO ===");

    Write("Nombre: ");
    var name = ReadLine() ?? "";

    Write("Username: ");
    var username = ReadLine() ?? "";

    Write("Email: ");
    var email = ReadLine() ?? "";

    Write("Calle: ");
    var street = ReadLine() ?? "";

    Write("Suite: ");
    var suite = ReadLine() ?? "";

    Write("Ciudad: ");
    var city = ReadLine() ?? "";

    Write("Código postal: ");
    var zipCode = ReadLine() ?? "";

    Write("Latitud: ");
    var lat = ReadLine() ?? "";

    Write("Longitud: ");
    var lng = ReadLine() ?? "";

    Write("Teléfono: ");
    var phone = ReadLine() ?? "";

    Write("Web: ");
    var website = ReadLine() ?? "";

    Write("Empresa: ");
    var companyName = ReadLine() ?? "";

    Write("CatchPhrase: ");
    var catchPhrase = ReadLine() ?? "";

    Write("BS: ");
    var bs = ReadLine() ?? "";

    var user = new User(
        0,
        name,
        username,
        email,
        new Address(
            street,
            suite,
            city,
            zipCode,
            new Geo(lat, lng)
        ),
        phone,
        website,
        new Company(
            companyName,
            catchPhrase,
            bs
        )
    );

    var resultCreate = await userService.CreateAsync(user.ToCreateRequest());

    if (resultCreate.IsFailure) {
        WriteLine($"Error: {resultCreate.Error.Message}");
        return;
    }

    WriteLine(
        $"Usuario creado correctamente con ID {resultCreate.Value.Id}.");
}

static async Task UpdateAsync(IUserService userService) {
    Write("Introduce el ID del usuario a actualizar: ");

    if (!int.TryParse(ReadLine(), out var id)) {
        WriteLine("El ID no es válido.");
        return;
    }

    WriteLine("=== NUEVOS DATOS ===");

    Write("Nombre: ");
    var name = ReadLine() ?? "";

    Write("Username: ");
    var username = ReadLine() ?? "";

    Write("Email: ");
    var email = ReadLine() ?? "";

    Write("Calle: ");
    var street = ReadLine() ?? "";

    Write("Suite: ");
    var suite = ReadLine() ?? "";

    Write("Ciudad: ");
    var city = ReadLine() ?? "";

    Write("Código postal: ");
    var zipCode = ReadLine() ?? "";

    Write("Latitud: ");
    var lat = ReadLine() ?? "";

    Write("Longitud: ");
    var lng = ReadLine() ?? "";

    Write("Teléfono: ");
    var phone = ReadLine() ?? "";

    Write("Web: ");
    var website = ReadLine() ?? "";

    Write("Empresa: ");
    var companyName = ReadLine() ?? "";

    Write("CatchPhrase: ");
    var catchPhrase = ReadLine() ?? "";

    Write("BS: ");
    var bs = ReadLine() ?? "";

    var user = new User(
        id,
        name,
        username,
        email,
        new Address(
            street,
            suite,
            city,
            zipCode,
            new Geo(lat, lng)
        ),
        phone,
        website,
        new Company(
            companyName,
            catchPhrase,
            bs
        )
    );

    var result = await userService.UpdateAsync(id, user.ToUpdateRequest());

    if (result.IsFailure) {
        WriteLine($"Error: {result.Error.Message}");
        return;
    }

    WriteLine($"Usuario {id} actualizado correctamente.");
}

static async Task DeleteAsync(IUserService userService) {
    Write("Introduce el ID del usuario a eliminar: ");

    if (!int.TryParse(ReadLine(), out var id)) {
        WriteLine("El ID no es válido.");
        return;
    }

    var result = await userService.DeleteAsync(id);

    if (result.IsFailure) {
        WriteLine($"Error: {result.Error.Message}");
        return;
    }

    WriteLine($"Usuario {id} eliminado correctamente.");
}

static async Task ExportAsync(IUserService userService) {
    WriteLine("Exportando usuarios...");

    var result = await userService.ExportToJsonAsync();

    if (result.IsFailure) {
        WriteLine($"Error: {result.Error.Message}");
        return;
    }

    WriteLine("Usuarios exportados correctamente.");
    WriteLine($"Fichero: {AppConfig.UsersJsonPath}");
}