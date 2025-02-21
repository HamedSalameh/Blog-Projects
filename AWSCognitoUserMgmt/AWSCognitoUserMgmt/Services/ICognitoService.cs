using Amazon.CognitoIdentityProvider.Model;

namespace AWSCognitoUserMgmt.IdentityProvider
{
    public interface ICognitoService
    {
        Task<AdminInitiateAuthResponse> AuthenticateUserAsync(string username, string password);
        Task<AdminCreateUserResponse> CreateUserAsync(string username, string password);
        Task<AdminDeleteUserResponse> DeleteUserAsync(string username);
        Task<AdminSetUserPasswordResponse> SetUserPasswordAsync(string username, string password);
    }
}