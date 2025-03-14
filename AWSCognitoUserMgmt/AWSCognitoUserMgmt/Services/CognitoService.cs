using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using AWSCognitoUserMgmt.IdentityProvider;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

public class CognitoService : ICognitoService
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly string _userPoolId;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ILogger<CognitoService> _logger;

    public CognitoService(IAmazonCognitoIdentityProvider cognitoClient,
                          IOptions<IdentityProviderConfiguration> options,
                          ILogger<CognitoService> logger)
    {
        _cognitoClient = cognitoClient ?? throw new ArgumentNullException(nameof(cognitoClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var config = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(config.PoolId))
            throw new ArgumentException("User pool ID cannot be null or empty", nameof(config.PoolId));

        if (string.IsNullOrWhiteSpace(config.ClientId))
            throw new ArgumentException("Client ID cannot be null or empty", nameof(config.ClientId));

        _userPoolId = config.PoolId;
        _clientId = config.ClientId;
        _clientSecret = config.ClientSecret;
    }

    /// <summary>
    /// Creates a new user in AWS Cognito
    /// </summary>
    public async Task<AdminCreateUserResponse> CreateUserAsync(string username, string password)
    {
        try
        {
            var request = new AdminCreateUserRequest
            {
                UserPoolId = _userPoolId,
                Username = username,
                TemporaryPassword = password,
                UserAttributes = new List<AttributeType>
                {
                    new AttributeType { Name = "email", Value = username }
                }
            };

            var response = await _cognitoClient.AdminCreateUserAsync(request).ConfigureAwait(false);
            _logger.LogInformation("User {Username} created successfully in Cognito.", username);
            return response;
        }
        catch (UsernameExistsException ex)
        {
            _logger.LogWarning(ex, "User {Username} already exists in Cognito.", username);
            throw;
        }
        catch (TooManyRequestsException ex)
        {
            _logger.LogError(ex, "Too many requests to AWS Cognito while creating user {Username}.", username);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating user {Username} in Cognito.", username);
            throw;
        }
    }

    public async Task<AdminCreateUserResponse> CreateUserWithAttributesAsync(string username, string password, Dictionary<string, string> attributes)
    {
        try
        {
            var request = new AdminCreateUserRequest
            {
                UserPoolId = _userPoolId,
                Username = username,
                TemporaryPassword = password,
                UserAttributes = attributes.Select(kvp => new AttributeType { Name = kvp.Key, Value = kvp.Value }).ToList()
            };
            var response = await _cognitoClient.AdminCreateUserAsync(request).ConfigureAwait(false);
            _logger.LogInformation("User {Username} created successfully in Cognito.", username);
            return response;
        }
        catch (UsernameExistsException ex)
        {
            _logger.LogWarning(ex, "User {Username} already exists in Cognito.", username);
            throw;
        }
        catch (TooManyRequestsException ex)
        {
            _logger.LogError(ex, "Too many requests to AWS Cognito while creating user {Username}.", username);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating user {Username} in Cognito.", username);
            throw;
        }
    }

    /// <summary>
    /// Authenticates a user and retrieves authentication tokens
    /// </summary>
    public async Task<AdminInitiateAuthResponse> AuthenticateUserAsync(string username, string password)
    {
        try
        {
            var request = new AdminInitiateAuthRequest
            {
                UserPoolId = _userPoolId,
                ClientId = _clientId,
                AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", username },
                    { "PASSWORD", password }
                }
            };

            var response = await _cognitoClient.AdminInitiateAuthAsync(request).ConfigureAwait(false);
            _logger.LogInformation("User {Username} authenticated successfully.", username);
            return response;
        }
        catch (NotAuthorizedException ex)
        {
            _logger.LogWarning(ex, "Invalid credentials for user {Username}.", username);
            throw;
        }
        catch (TooManyRequestsException ex)
        {
            _logger.LogError(ex, "Too many requests to AWS Cognito while authenticating user {Username}.", username);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while authenticating user {Username}.", username);
            throw;
        }
    }

    /// <summary>
    /// Sets a user's password in AWS Cognito
    /// </summary>
    public async Task<AdminSetUserPasswordResponse> SetUserPasswordAsync(string username, string password)
    {
        try
        {
            var request = new AdminSetUserPasswordRequest
            {
                UserPoolId = _userPoolId,
                Username = username,
                Password = password,
                Permanent = true
            };

            var response = await _cognitoClient.AdminSetUserPasswordAsync(request).ConfigureAwait(false);
            _logger.LogInformation("Password set successfully for user {Username}.", username);
            return response;
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "Attempted to set password for non-existent user {Username}.", username);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while setting password for user {Username}.", username);
            throw;
        }
    }

    /// <summary>
    /// Deletes a user from AWS Cognito
    /// </summary>
    public async Task<AdminDeleteUserResponse> DeleteUserAsync(string username)
    {
        try
        {
            var request = new AdminDeleteUserRequest
            {
                UserPoolId = _userPoolId,
                Username = username
            };

            var response = await _cognitoClient.AdminDeleteUserAsync(request).ConfigureAwait(false);
            _logger.LogInformation("User {Username} deleted successfully from Cognito.", username);
            return response;
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "Attempted to delete non-existent user {Username}.", username);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting user {Username} from Cognito.", username);
            throw;
        }
    }

    public async Task<AdminUpdateUserAttributesResponse> UpdateUserAttributesAsync(string username, Dictionary<string, string> attributes)
    {
        try
        {
            var request = new AdminUpdateUserAttributesRequest
            {
                UserPoolId = _userPoolId,
                Username = username,
                UserAttributes = attributes.Select(kvp => new AttributeType { Name = kvp.Key, Value = kvp.Value }).ToList()
            };
            var response = await _cognitoClient.AdminUpdateUserAttributesAsync(request).ConfigureAwait(false);
            _logger.LogInformation("User {Username} attributes updated successfully in Cognito.", username);
            return response;
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "Attempted to update attributes for non-existent user {Username}.", username);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating attributes for user {Username} in Cognito.", username);
            throw;
        }
    }

    public async Task<AdminGetUserResponse> GetUserAsync(string username)
    {
        try
        {
            var request = new AdminGetUserRequest
            {
                UserPoolId = _userPoolId,
                Username = username
            };
            var response = await _cognitoClient.AdminGetUserAsync(request).ConfigureAwait(false);
            _logger.LogInformation("User {Username} retrieved successfully from Cognito.", username);
            return response;
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "Attempted to retrieve non-existent user {Username}.", username);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving user {Username} from Cognito.", username);
            throw;
        }
    }

    // Search all users with certain family_name
    public async Task<ListUsersResponse> ListUsersAsync(string familyName)
    {
        try
        {
            var request = new ListUsersRequest
            {
                UserPoolId = _userPoolId,
                Filter = $"family_name = \"{familyName}\""
            };
            var response = await _cognitoClient.ListUsersAsync(request).ConfigureAwait(false);
            _logger.LogInformation("Users with family_name {FamilyName} retrieved successfully from Cognito.", familyName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving users with family_name {FamilyName} from Cognito.", familyName);
            throw;
        }
    }

    // search all users with certain email and family_name
    public async Task<ListUsersResponse> ListUsersAsync(string email, string familyName)
    {
        try
        {
            var request = new ListUsersRequest
            {
                UserPoolId = _userPoolId,
                Filter = $"email = \"{email}\" and family_name = \"{familyName}\""
            };
            var response = await _cognitoClient.ListUsersAsync(request).ConfigureAwait(false);
            _logger.LogInformation("Users with email {Email} and family_name {FamilyName} retrieved successfully from Cognito.", email, familyName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving users with email {Email} and family_name {FamilyName} from Cognito.", email, familyName);
            throw;
        }
    }

    // Fetch all users in a pool
    public async Task<ListUsersResponse> ListUsersAsync()
    {
        try
        {
            var request = new ListUsersRequest
            {
                UserPoolId = _userPoolId
            };
            var response = await _cognitoClient.ListUsersAsync(request).ConfigureAwait(false);
            _logger.LogInformation("All users retrieved successfully from Cognito.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving all users from Cognito.");
            throw;
        }
    }

    // additional management API's

    // Reset user password
    public async Task<AdminResetUserPasswordResponse> ResetUserPasswordAsync(string username)
    {
        try
        {
            var request = new AdminResetUserPasswordRequest
            {
                UserPoolId = _userPoolId,
                Username = username
            };
            var response = await _cognitoClient.AdminResetUserPasswordAsync(request).ConfigureAwait(false);
            _logger.LogInformation("Password reset successfully for user {Username}.", username);
            return response;
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "Attempted to reset password for non-existent user {Username}.", username);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while resetting password for user {Username}.", username);
            throw;
        }
    }

    // Disable user
    public async Task<AdminDisableUserResponse> DisableUserAsync(string username)
    {
        try
        {
            var request = new AdminDisableUserRequest
            {
                UserPoolId = _userPoolId,
                Username = username
            };
            var response = await _cognitoClient.AdminDisableUserAsync(request).ConfigureAwait(false);
            _logger.LogInformation("User {Username} disabled successfully in Cognito.", username);
            return response;
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "Attempted to disable non-existent user {Username}.", username);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while disabling user {Username} in Cognito.", username);
            throw;
        }
    }

    // Create a group
    public async Task<CreateGroupResponse> CreateGroupAsync(string groupName, string description)
    {
        try
        {
            var request = new CreateGroupRequest
            {
                UserPoolId = _userPoolId,
                GroupName = groupName,
                Description = description
            };
            var response = await _cognitoClient.CreateGroupAsync(request).ConfigureAwait(false);
            _logger.LogInformation("Group {GroupName} created successfully in Cognito.", groupName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating group {GroupName} in Cognito.", groupName);
            throw;
        }
    }

    // Add user to a group
    public async Task<AdminAddUserToGroupResponse> AddUserToGroupAsync(string username, string groupName)
    {
        try
        {
            var request = new AdminAddUserToGroupRequest
            {
                UserPoolId = _userPoolId,
                Username = username,
                GroupName = groupName
            };
            var response = await _cognitoClient.AdminAddUserToGroupAsync(request).ConfigureAwait(false);
            _logger.LogInformation("User {Username} added to group {GroupName} successfully in Cognito.", username, groupName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while adding user {Username} to group {GroupName} in Cognito.", username, groupName);
            throw;
        }
    }

    // remove user from a group
    public async Task<AdminRemoveUserFromGroupResponse> RemoveUserFromGroupAsync(string username, string groupName)
    {
        try
        {
            var request = new AdminRemoveUserFromGroupRequest
            {
                UserPoolId = _userPoolId,
                Username = username,
                GroupName = groupName
            };
            var response = await _cognitoClient.AdminRemoveUserFromGroupAsync(request).ConfigureAwait(false);
            _logger.LogInformation("User {Username} removed from group {GroupName} successfully in Cognito.", username, groupName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while removing user {Username} from group {GroupName} in Cognito.", username, groupName);
            throw;
        }
    }

    // Delete a group
    public async Task<DeleteGroupResponse> DeleteGroupAsync(string groupName)
    {
        try
        {
            var request = new DeleteGroupRequest
            {
                UserPoolId = _userPoolId,
                GroupName = groupName
            };
            var response = await _cognitoClient.DeleteGroupAsync(request).ConfigureAwait(false);
            _logger.LogInformation("Group {GroupName} deleted successfully in Cognito.", groupName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting group {GroupName} in Cognito.", groupName);
            throw;
        }
    }

    // Authentication

    // Sign in user using credentials
    public async Task<AdminInitiateAuthResponse> SignInUserAsync(string username, string password)
    {
        try
        {
            var request = new AdminInitiateAuthRequest
            {
                UserPoolId = _userPoolId,
                ClientId = _clientId,
                AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", username },
                    { "PASSWORD", password },
                    // Secret hash is required for admin-initiated auth
                    { "SECRET_HASH", GetSecretHash(username, _clientId, _clientSecret) }
                }
            };
            var response = await _cognitoClient.AdminInitiateAuthAsync(request).ConfigureAwait(false);
            _logger.LogInformation("User {Username} signed in successfully.", username);
            return response;
        }
        catch (NotAuthorizedException ex)
        {
            _logger.LogWarning(ex, "Invalid credentials for user {Username}.", username);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while signing in user {Username}.", username);
            throw;
        }
    }

    public async Task<InitiateAuthResponse> SignInUserAsyncWithPasswordAuth(string username, string password)
    {
        var authRequest = new InitiateAuthRequest
        {
            AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
            ClientId = _clientId,
            AuthParameters = new Dictionary<string, string>
            {
                { "USERNAME", username },
                { "PASSWORD", password },
                { "SECRET_HASH", CalculateSecretHash(username) }
            }
        };

        try
        {
            var authResponse = await _cognitoClient.InitiateAuthAsync(authRequest);
            return authResponse;
        }
        catch (NotAuthorizedException)
        {
            Console.WriteLine("The username or password is incorrect.");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            throw;
        }
    }

    private string CalculateSecretHash(string username)
    {
        var key = Encoding.UTF8.GetBytes(_clientSecret);
        using (var hmac = new HMACSHA256(key))
        {
            var message = Encoding.UTF8.GetBytes(username + _clientId);
            var hash = hmac.ComputeHash(message);
            return Convert.ToBase64String(hash);
        }
    }

    private static string GetSecretHash(string username, string clientId, string clientSecret)
    {
        var secretBlock = username + clientId;
        var keyBytes = Encoding.UTF8.GetBytes(clientSecret);
        using (var hmac = new HMACSHA256(keyBytes))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(secretBlock));
            return Convert.ToBase64String(hash);
        }
    }

    // Enable MFA for user
    public async Task<AdminSetUserMFAPreferenceResponse> EnableMFAForUserAsync(string username)
    {
        try
        {
            var request = new AdminSetUserMFAPreferenceRequest
            {
                UserPoolId = _userPoolId,
                Username = username,
                // set MFA preference to 'SOFTWARE_TOKEN_MFA'
                SMSMfaSettings = new SMSMfaSettingsType { Enabled = false },
                SoftwareTokenMfaSettings = new SoftwareTokenMfaSettingsType { Enabled = true, PreferredMfa = true }

            };
            var response = await _cognitoClient.AdminSetUserMFAPreferenceAsync(request).ConfigureAwait(false);
            _logger.LogInformation("MFA enabled successfully for user {Username}.", username);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while enabling MFA for user {Username}.", username);
            throw;
        }
    }

    // Sign out user
    public async Task<AdminUserGlobalSignOutResponse> SignOutUserAsync(string username)
    {
        try
        {
            var request = new AdminUserGlobalSignOutRequest
            {
                UserPoolId = _userPoolId,
                Username = username
            };
            var response = await _cognitoClient.AdminUserGlobalSignOutAsync(request).ConfigureAwait(false);
            _logger.LogInformation("User {Username} signed out successfully.", username);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while signing out user {Username}.", username);
            throw;
        }
    }
}

internal class CognitoHelper
{
    

}