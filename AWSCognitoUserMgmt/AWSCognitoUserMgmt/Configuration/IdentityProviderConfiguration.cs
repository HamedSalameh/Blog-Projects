using Microsoft.Extensions.Configuration;

public class IdentityProviderConfiguration
{
    public string Region { get; set; } = string.Empty;
    public string PoolId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
