using Amazon.CognitoIdentityProvider.Model;

namespace AWSCognitoUserMgmt.IdentityProvider
{
    public interface ICognitoService
    {
        Task<AdminAddUserToGroupResponse> AddUserToGroupAsync(string username, string groupName);
        Task<AdminInitiateAuthResponse> AuthenticateUserAsync(string username, string password);
        Task<CreateGroupResponse> CreateGroupAsync(string groupName, string description);
        Task<AdminCreateUserResponse> CreateUserAsync(string username, string password);
        Task<DeleteGroupResponse> DeleteGroupAsync(string groupName);
        Task<AdminDeleteUserResponse> DeleteUserAsync(string username);
        Task<AdminDisableUserResponse> DisableUserAsync(string username);
        Task<AdminSetUserMFAPreferenceResponse> EnableMFAForUserAsync(string username);
        Task<AdminGetUserResponse> GetUserAsync(string username);
        Task<ListUsersResponse> ListUsersAsync();
        Task<ListUsersResponse> ListUsersAsync(string email, string familyName);
        Task<ListUsersResponse> ListUsersAsync(string familyName);
        Task<AdminRemoveUserFromGroupResponse> RemoveUserFromGroupAsync(string username, string groupName);
        Task<AdminResetUserPasswordResponse> ResetUserPasswordAsync(string username);
        Task<AdminSetUserPasswordResponse> SetUserPasswordAsync(string username, string password);
        Task<AdminInitiateAuthResponse> SignInUserAsync(string username, string password);
        Task<AdminUpdateUserAttributesResponse> UpdateUserAttributesAsync(string username, Dictionary<string, string> attributes);
    }
}