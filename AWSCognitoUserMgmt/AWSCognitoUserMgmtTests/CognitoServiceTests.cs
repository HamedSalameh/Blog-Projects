using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using AWSCognitoUserMgmt.IdentityProvider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;

namespace AWSCognitoUserMgmtTests
{
    public class CognitoServiceTests
    {
        private Mock<IAmazonCognitoIdentityProvider> _mockCognitoClient;
        private Mock<IOptions<IdentityProviderConfiguration>> _mockOptions;
        private Mock<ILogger<CognitoService>> _mockLogger;
        private CognitoService _cognitoService;

        private readonly string _testUsername = "testuser@example.com";
        private readonly string _testPassword = "TempPassword123!";

        [SetUp]
        public void SetUp()
        {
            _mockCognitoClient = new Mock<IAmazonCognitoIdentityProvider>();
            _mockOptions = new Mock<IOptions<IdentityProviderConfiguration>>();
            _mockLogger = new Mock<ILogger<CognitoService>>();

            // Set up IdentityProviderConfiguration values
            var identityProviderConfig = new IdentityProviderConfiguration
            {
                PoolId = "test-pool-id",
                ClientId = "test-client-id"
            };
            _mockOptions.Setup(o => o.Value).Returns(identityProviderConfig);

            // Instantiate CognitoService with mocks
            _cognitoService = new CognitoService(
                _mockCognitoClient.Object,
                _mockOptions.Object,
                _mockLogger.Object
            );
        }

        [Test]
        public async Task CreateUserAsync_ShouldReturnSuccess_WhenUserIsCreated()
        {
            // Arrange
            var expectedResponse = new AdminCreateUserResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            };

            _mockCognitoClient
                .Setup(client => client.AdminCreateUserAsync(It.IsAny<AdminCreateUserRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _cognitoService.CreateUserAsync(_testUsername, _testPassword);

            // Assert
            result.ShouldNotBeNull();
            result.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

            _mockCognitoClient.Verify(client =>
                client.AdminCreateUserAsync(It.IsAny<AdminCreateUserRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task CreateUserAsync_ShouldThrowException_WhenUsernameExists()
        {
            // Arrange
            var exception = new UsernameExistsException("User already exists");
            _mockCognitoClient
                .Setup(client => client.AdminCreateUserAsync(It.IsAny<AdminCreateUserRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var ex = await Should.ThrowAsync<UsernameExistsException>(() =>
                _cognitoService.CreateUserAsync(_testUsername, _testPassword));

            ex.Message.ShouldBe("User already exists");

            _mockCognitoClient.Verify(client =>
                client.AdminCreateUserAsync(It.IsAny<AdminCreateUserRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task CreateUserAsync_ShouldThrowException_WhenTooManyRequests()
        {
            // Arrange
            var exception = new TooManyRequestsException("Too many requests");
            _mockCognitoClient
                .Setup(client => client.AdminCreateUserAsync(It.IsAny<AdminCreateUserRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var ex = await Should.ThrowAsync<TooManyRequestsException>(() =>
                _cognitoService.CreateUserAsync(_testUsername, _testPassword));

            ex.Message.ShouldBe("Too many requests");

            _mockCognitoClient.Verify(client =>
                client.AdminCreateUserAsync(It.IsAny<AdminCreateUserRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task CreateUserAsync_ShouldThrowGenericException_WhenUnexpectedErrorOccurs()
        {
            // Arrange
            var exception = new Exception("Unexpected error");
            _mockCognitoClient
                .Setup(client => client.AdminCreateUserAsync(It.IsAny<AdminCreateUserRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var ex = await Should.ThrowAsync<Exception>(() =>
                _cognitoService.CreateUserAsync(_testUsername, _testPassword));

            ex.Message.ShouldBe("Unexpected error");

            _mockCognitoClient.Verify(client =>
                client.AdminCreateUserAsync(It.IsAny<AdminCreateUserRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}