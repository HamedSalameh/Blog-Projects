using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using AWSCognitoUserMgmt.IdentityProvider;
using Microsoft.Extensions.Options;

public class CognitoService : ICognitoService
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly string _userPoolId;
    private readonly string _clientId;
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
}
