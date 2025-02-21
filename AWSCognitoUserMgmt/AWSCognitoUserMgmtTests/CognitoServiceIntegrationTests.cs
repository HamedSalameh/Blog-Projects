using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using DotNetEnv;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSCognitoUserMgmtTests
{
    public class CognitoServiceIntegrationTests
    {
        private IAmazonCognitoIdentityProvider _cognitoClient;
        private CognitoService _cognitoService;

        private readonly string _testUsername = $"testuser-{Guid.NewGuid()}@example.com";
        private const string _testPassword = "TempPassword123!";
        private string _userPoolId;
        private string _clientId;

        [SetUp]
        public void SetUp()
        {
            // Load environment variables from .env file
            var envFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(envFilePath))
            {
                Env.Load(envFilePath);
            }

            // Load configuration from environment variables or appsettings.json
            _userPoolId = Environment.GetEnvironmentVariable("AWS_COGNITO_POOL_ID") ?? throw new Exception("Missing User Pool ID");
            _clientId = Environment.GetEnvironmentVariable("AWS_COGNITO_CLIENT_ID") ?? throw new Exception("Missing Client ID");

            var config = new IdentityProviderConfiguration
            {
                PoolId = _userPoolId,
                ClientId = _clientId
            };

            // Initialize the Cognito Client
            _cognitoClient = new AmazonCognitoIdentityProviderClient(RegionEndpoint.USEast1);

            // Create CognitoService instance with actual AWS Cognito
            var options = Options.Create(config);
            var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<CognitoService>();

            _cognitoService = new CognitoService(_cognitoClient, options, logger);
        }

        [Test]
        public async Task CreateUserAsync_ShouldCreateUserInCognito()
        {
            // Act
            var response = await _cognitoService.CreateUserAsync(_testUsername, _testPassword);

            // Assert
            response.ShouldNotBeNull();
            response.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            response.User.Attributes.FirstOrDefault(x => x.Name.Equals("email"))?.Value.ShouldBe(_testUsername);

            Console.WriteLine($"User {_testUsername} created successfully.");
        }

        /// <summary>
        /// Test setting a user's password in AWS Cognito.
        /// </summary>
        [Test]
        public async Task SetUserPasswordAsync_ShouldUpdatePasswordInCognito()
        {
            // Arrange: Ensure the user exists
            await _cognitoService.CreateUserAsync(_testUsername, _testPassword);
            string newPassword = "NewSecurePassword123!";

            // Act
            var response = await _cognitoService.SetUserPasswordAsync(_testUsername, newPassword);

            // Assert
            response.ShouldNotBeNull();
            response.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

            Console.WriteLine($"Password updated successfully for {_testUsername}.");
        }

        /// <summary>
        /// Test deleting a user in AWS Cognito.
        /// </summary>
        [Test]
        public async Task DeleteUserAsync_ShouldDeleteUserFromCognito()
        {
            // Arrange: Ensure the user exists
            await _cognitoService.CreateUserAsync(_testUsername, _testPassword);

            // Act
            var response = await _cognitoService.DeleteUserAsync(_testUsername);

            // Assert
            response.ShouldNotBeNull();
            response.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

            Console.WriteLine($"User {_testUsername} deleted successfully.");
        }

        [TearDown]
        public async Task TearDown()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_testUsername))
                {
                    var request = new AdminDeleteUserRequest
                    {
                        UserPoolId = _userPoolId,
                        Username = _testUsername
                    };

                    await _cognitoClient.AdminDeleteUserAsync(request);

                    _cognitoClient.Dispose();
                    Console.WriteLine($"User {_testUsername} deleted successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete test user: {ex.Message}");
            }
        }
    }
}
