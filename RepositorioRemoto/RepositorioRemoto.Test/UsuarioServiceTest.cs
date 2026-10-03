using RepositorioRemoto.Api;
using RepositorioRemoto.Models;
using RepositorioRemoto.Service;
using RepositorioRemoto.Dto;
using RepositorioRemoto.Errors;

namespace RepositorioRemoto.Test.TestUnitariosCorrectos;
using FluentAssertions;
using Moq;
using Refit;

[TestFixture]
public class UsuarioServiceTest {
    private Mock<IJsonPlaceholderApi> _mockApi = null!;
    private UsuarioService _service = null!;

    [SetUp]
    public void SetUp() {
        _mockApi = new Mock<IJsonPlaceholderApi>();
        _service = new UsuarioService(_mockApi.Object);
    }

    [TestFixture]
    public class CasosCorrectosService : UsuarioServiceTest {
        [Test]
        public async Task GetAllAsync_DeberiaRetornarTodosLosUsuarios() {

            //Arrange
            var usuarios = new List<Usuario> {
                new(1, "Adrián", "adri", "adri@gmail.com"),
                new(2, "Alvaro", "alvaro", "alvaro@gmail.com")
            };
            _mockApi.Setup(a => a.GetUsuariosAsync()).ReturnsAsync(usuarios);

            //Act
            var resultado = await _service.GetAllAsync();

            //Assert
            resultado.Should().HaveCount(2);
            resultado[0].Name.Should().Be("Adrián");

        }

        [Test]
        public async Task GetByIdAsync_Existe_DebeRetornarSuccess() {
            // Arrange
            var usuario = new Usuario(1, "Adrián", "adri", "adri@gmail.com");
            _mockApi.Setup(a => a.GetUsuarioByIdAsync(1)).ReturnsAsync(usuario);
            
            //Act
            var resultado = await _service.GetByIdAsync(1);
            
            //Assert
            resultado.Should().BeOfType<Result<Usuario, DomainErrors>.Success>();
            if (resultado is Result<Usuario, DomainErrors>.Success success) {
                success.Value.Name.Should().Be("Adrián");
            }
        }

        [Test]
        public async Task CreateAsync_DatosValidos_DeberianRetornarSuccess() {
            //Arrange
            var request = new CreateUserRequest("Adrián", "adri", "adri@gmail.com");
            var creado = new Usuario(1, "Adrián", "adri", "adri@gmail.com");
            _mockApi.Setup(a => a.CreateUsuarioAsync(request)).ReturnsAsync(creado);
            
            //Act
            var resultado = await _service.CreateAsync(request);
            
            //Assert
            resultado.Should().BeOfType<Result<Usuario, DomainErrors>.Success>();
            if (resultado is Result<Usuario, DomainErrors>.Success success) {
                success.Value.Id.Should().Be(1);
            }
        }

        [Test]
        public async Task DeleteAsync_Eiste_DeberiaRetornarSuccess() {
            //Arrange
            _mockApi.Setup(a => a.DeleteUsuarioAsync(1)).Returns(Task.CompletedTask);
            
            //Act
            var resultado = await _service.DeleteAsync(1);
            
            //Assert
            resultado.Should().BeOfType<Result<bool, DomainErrors>.Success>();
        }
    }

    [TestFixture]
    public class CasosIncorrectosService : UsuarioServiceTest {

        [Test]
        public async Task GetByIdAsync_Inexistente_DeberianRetornarFailure() {
            //Arrange
            _mockApi.Setup(a => a.GetUsuarioByIdAsync(999)).ReturnsAsync((Usuario?)null);
            
            //Act
            var resultado = await _service.GetByIdAsync(999);
            
            //Assert
            resultado.Should().BeOfType<Result<Usuario, DomainErrors>.Failure>();
            if (resultado is Result<Usuario, DomainErrors>.Failure failure) {
                failure.Error.Should().BeOfType<DomainErrors.NotFound>();
            }
        }

        [Test]
        public async Task CreateAsync_NombreVacio_DeberiaRetornarValidationError() {
            //Arrange
            var request = new CreateUserRequest("", "adri", "adri@gmail.com");
            
            //Act
            var resultado = await _service.CreateAsync(request);
            
            //Assert
            resultado.Should().BeOfType<Result<Usuario, DomainErrors>.Failure>();
            if (resultado is Result<Usuario, DomainErrors>.Failure failure) {
                failure.Error.Should().BeOfType<DomainErrors.ValidationError>();
            }

        }
        
        [Test]
        public async Task CreateAsync_EmailVacio_DeberiaRetornarValidationError()
        {
            // Arrange
            var request = new CreateUserRequest("Adrián", "adri", "");

            // Act
            var resultado = await _service.CreateAsync(request);

            // Assert
            resultado.Should().BeOfType<Result<Usuario, DomainErrors>.Failure>();
            if (resultado is Result<Usuario, DomainErrors>.Failure failure)
            {
                failure.Error.Should().BeOfType<DomainErrors.ValidationError>();
            }
        }
    }
}