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
        private const string _testUser1_GivenName = "Test1Given";
        private const string _testUser1_FamilyName = "User1Family";
        private const string _testUser1_Email = "user1@example.com";

        private const string _testUser2_GivenName = "Test2Given";
        private const string _testUser2_FamilyName = "User2Family";
        private const string _testUser2_Email = "user2@example.com";

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
            var _clientSecet = Environment.GetEnvironmentVariable("AWS_COGNITO_CLIENT_SECRET") ?? throw new Exception("Missing Client Secret");

            var config = new IdentityProviderConfiguration
            {
                PoolId = _userPoolId,
                ClientId = _clientId,
                ClientSecret = _clientSecet
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

        [Test]
        public async Task GetUserAsync_ShouldReturnUserFromCognito()
        {
            // Arrange: Ensure the user exists
            await _cognitoService.CreateUserAsync(_testUsername, _testPassword);
            // Act
            var response = await _cognitoService.GetUserAsync(_testUsername);
            // Assert
            response.ShouldNotBeNull();
            // assert against email
            response.UserAttributes.FirstOrDefault(x => x.Name.Equals("email"))?.Value.ShouldBe(_testUsername);
            Console.WriteLine($"User {_testUsername} retrieved successfully.");
        }
        
        [Test]
        public async Task GetUserAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // assert that throws UserNotFoundException
            Assert.ThrowsAsync<UserNotFoundException>(async () => await _cognitoService.GetUserAsync(_testUsername));
        }

        // Search users by email
        [Test]
        public async Task SearchUsersByEmailAsync_ShouldReturnUsersFromCognito()
        {
            // Arrange: Ensure the user exists
            await _cognitoService.CreateUserWithAttributesAsync(_testUser1_Email, _testPassword, new Dictionary<string, string>
            {
                { "given_name", _testUser1_GivenName },
                { "family_name", _testUser1_FamilyName }
            });

            await _cognitoService.CreateUserWithAttributesAsync(_testUser2_Email, _testPassword, new Dictionary<string, string>
            {
                { "given_name", _testUser2_GivenName },
                { "family_name", _testUser2_FamilyName }
            });

            // Act
            var response = await _cognitoService.ListUsersAsync(_testUser1_FamilyName);
            // Assert
            response.ShouldNotBeNull();
            response.Users.Count.ShouldBeGreaterThan(0);
            // assert against email
            response.Users.FirstOrDefault()?.Attributes.FirstOrDefault(x => x.Name.Equals("email"))?.Value.ShouldBe(_testUser1_Email);
            Console.WriteLine($"User {_testUsername} retrieved successfully.");
        }

        // search all users in the pool
        [Test]
        public async Task ListUsersAsync_ShouldReturnUsersFromCognito()
        {
            // Arrange: Ensure the user exists
            await _cognitoService.CreateUserWithAttributesAsync(_testUser1_Email, _testPassword, new Dictionary<string, string>
            {
                { "given_name", _testUser1_GivenName },
                { "family_name", _testUser1_FamilyName }
            });
            await _cognitoService.CreateUserWithAttributesAsync(_testUser2_Email, _testPassword, new Dictionary<string, string>
            {
                { "given_name", _testUser2_GivenName },
                { "family_name", _testUser2_FamilyName }
            });
            // Act
            var response = await _cognitoService.ListUsersAsync();
            // Assert
            response.ShouldNotBeNull();
            response.Users.Count.ShouldBeGreaterThan(0);
            response.Users.Count.ShouldBe(2);
            // assert against email
            
            Console.WriteLine($"User {_testUsername} retrieved successfully.");
        }

        // Update user attribute (family name) test
        [Test]
        public async Task UpdateUserAttributeAsync_ShouldUpdateUserAttributeInCognito()
        {
            // Arrange: Ensure the user exists
            await _cognitoService.CreateUserWithAttributesAsync(_testUser1_Email, _testPassword, new Dictionary<string, string>
            {
                { "given_name", _testUser1_GivenName },
                { "family_name", _testUser1_FamilyName }
            });
            string newFamilyName = "NewFamilyName";

            var newAttributes = new Dictionary<string, string>
            {
                { "family_name", newFamilyName }
            };
            // Act
            var response = await _cognitoService.UpdateUserAttributesAsync(_testUser1_Email, newAttributes);
            // Assert
            response.ShouldNotBeNull();
            response.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            Console.WriteLine($"User {_testUser1_Email} updated successfully.");

            // delete the user
            await _cognitoService.DeleteUserAsync(_testUser1_Email);
        }

        // Set password and then delete user
        [Test]
        public async Task SetPasswordAndDeleteUserAsync_ShouldSetPasswordAndDeleteUserFromCognito()
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
            // Act
            var deleteResponse = await _cognitoService.DeleteUserAsync(_testUsername);
            // Assert
            deleteResponse.ShouldNotBeNull();
            deleteResponse.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            Console.WriteLine($"User {_testUsername} deleted successfully.");
        }

        // Disable user test
        [Test]
        public async Task DisableUserAsync_ShouldDisableUserInCognito()
        {
            // Arrange: Ensure the user exists
            await _cognitoService.CreateUserAsync(_testUsername, _testPassword);
            // Act
            var response = await _cognitoService.DisableUserAsync(_testUsername);
            // Assert
            response.ShouldNotBeNull();
            response.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            Console.WriteLine($"User {_testUsername} disabled successfully.");
        }

        // Create a group test
        [Test]
        public async Task CreateGroupAsync_ShouldCreateGroupInCognito()
        {
            string groupName = "TestGroup";
            // Act
            var response = await _cognitoService.CreateGroupAsync(groupName, "Test Group Description");
            // Assert
            response.ShouldNotBeNull();
            response.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            Console.WriteLine($"Group {groupName} created successfully.");
        }

        // Delete a group test
        [Test]
        public async Task DeleteGroupAsync_ShouldDeleteGroupFromCognito()
        {
            string groupName = "TestGroup";
            // Arrange: Ensure the group exists
            await _cognitoService.CreateGroupAsync(groupName, "Test Group Description");
            // Act
            var response = await _cognitoService.DeleteGroupAsync(groupName);
            // Assert
            response.ShouldNotBeNull();
            response.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            Console.WriteLine($"Group {groupName} deleted successfully.");
        }

        // add user to group test
        [Test]
        public async Task AddUserToGroupAsync_ShouldAddUserToGroupInCognito()
        {
            string groupName = "TestGroup";
            // Arrange: Ensure the user exists
            await _cognitoService.CreateUserAsync(_testUsername, _testPassword);
            // Arrange: Ensure the group exists
            await _cognitoService.CreateGroupAsync(groupName, "Test Group Description");
            // Act
            var response = await _cognitoService.AddUserToGroupAsync(_testUsername, groupName);
            // Assert
            response.ShouldNotBeNull();
            response.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            Console.WriteLine($"User {_testUsername} added to group {groupName} successfully.");

            // remove user from group
            await _cognitoService.RemoveUserFromGroupAsync(_testUsername, groupName);
            // delete the user
            await _cognitoService.DeleteUserAsync(_testUsername);
            // delete the group
            await _cognitoService.DeleteGroupAsync(groupName);
        }

        // Create a user, sign in then sign out, and delete the user
        [Test]
        public async Task SignInAndSignOutUserAsync_ShouldSignInAndSignOutUserFromCognito()
        {
            // Arrange: Ensure the user exists
            await _cognitoService.CreateUserAsync(_testUsername, _testPassword);
            // Act
            // Change the password to the correct one
            var changePasswordResponse = await _cognitoService.SetUserPasswordAsync(_testUsername, "P@55word01!");
            try
            {
                var signInResponse = await _cognitoService.SignInUserAsync(_testUsername, "P@55word01!");
                // Assert
                signInResponse.ShouldNotBeNull();
                signInResponse.AuthenticationResult.ShouldNotBeNull();
                signInResponse.AuthenticationResult.AccessToken.ShouldNotBeNullOrEmpty();
                Console.WriteLine($"User {_testUsername} signed in successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to sign in user {_testUsername}: {ex.Message}");
                throw;
            }
            
            // Act
            var signOutResponse = await _cognitoService.SignOutUserAsync(_testUsername);
            // Assert
            signOutResponse.ShouldNotBeNull();
            signOutResponse.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            Console.WriteLine($"User {_testUsername} signed out successfully.");
            // delete the user
            await _cognitoService.DeleteUserAsync(_testUsername);
        }

        // testing SignInUserAsyncWithPasswordAuth
        [Test]
        public async Task SignInUserAsyncWithPasswordAuth_ShouldSignInUserFromCognito()
        {
            // Arrange: Ensure the user exists
            await _cognitoService.CreateUserAsync(_testUsername, _testPassword);
            // Act
            // Change the password to the correct one
            var changePasswordResponse = await _cognitoService.SetUserPasswordAsync(_testUsername, "P@55word01!");
            try
            {
                var signInResponse = await _cognitoService.SignInUserAsyncWithPasswordAuth(_testUsername, "P@55word01!");
                // Assert
                signInResponse.ShouldNotBeNull();
                signInResponse.AuthenticationResult.ShouldNotBeNull();
                signInResponse.AuthenticationResult.AccessToken.ShouldNotBeNullOrEmpty();
                Console.WriteLine($"User {_testUsername} signed in successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to sign in user {_testUsername}: {ex.Message}");
                throw;
            }
            // delete the user
            await _cognitoService.DeleteUserAsync(_testUsername);
        }

        // Testing signout user
        [Test]
        public async Task SignOutUserAsync_ShouldSignOutUserFromCognito()
        {
            // Arrange: Ensure the user exists
            await _cognitoService.CreateUserAsync(_testUsername, _testPassword);
            // Act
            // Change the password to the correct one
            var changePasswordResponse = await _cognitoService.SetUserPasswordAsync(_testUsername, "P@55word01!");
            try
            {
                var signInResponse = await _cognitoService.SignInUserAsync(_testUsername, "P@55word01!");
                // Assert
                signInResponse.ShouldNotBeNull();
                signInResponse.AuthenticationResult.ShouldNotBeNull();
                signInResponse.AuthenticationResult.AccessToken.ShouldNotBeNullOrEmpty();
                Console.WriteLine($"User {_testUsername} signed in successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to sign in user {_testUsername}: {ex.Message}");
                throw;
            }
            // Act
            try
            {
                var signOutResponse = await _cognitoService.SignOutUserAsync(_testUsername);
                // Assert
                signOutResponse.ShouldNotBeNull();
                signOutResponse.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to sign out user {_testUsername}: {exception.Message}");
                throw;
            }
            Console.WriteLine($"User {_testUsername} signed out successfully.");
            // delete the user
            await _cognitoService.DeleteUserAsync(_testUsername);
        }

        // Test scenario: Create a user with MFA enabled, sign in with MFA, and delete the user
        [Test]
        public async Task SignInUserWithMFAAsync_ShouldSignInUserWithMFAFromCognito()
        {
            // Arrange: Ensure the user exists
            await _cognitoService.CreateUserAsync(_testUsername, _testPassword);
            // Act
            // Change the password to the correct one
            var changePasswordResponse = await _cognitoService.SetUserPasswordAsync(_testUsername, "P@55word01!");
            try
            {
                var enableMFAresponse = await _cognitoService.EnableMFAForUserAsync(_testUsername);
                // Assert
                enableMFAresponse.ShouldNotBeNull();
                enableMFAresponse.HttpStatusCode.ShouldBe(System.Net.HttpStatusCode.OK);


                Console.WriteLine($"User {_testUsername} signed in successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to sign in user {_testUsername}: {ex.Message}");
                throw;
            }
            // delete the user
            await _cognitoService.DeleteUserAsync(_testUsername);
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
