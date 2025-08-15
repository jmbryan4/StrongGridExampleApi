using Microsoft.Extensions.Options;

namespace StrongGridMinimalApi.Services;

public sealed class VaultService(IOptions<SecretsVaultOptions> options) : IVaultService
{
    public async Task<string?> GetSecretAsync(string secretPath)
    {
        await Task.Delay(50); // Simulate network latency
        // fetches from options config, but a real implementation would fetch from a secure vault or db
        // e.g. HashiCorp Vault, Azure Key Vault, AWS Secrets Manager, etc.
        return options.Value.Secrets.GetValueOrDefault(secretPath);
    }
}

public sealed record SecretsVaultOptions
{
    public const string SectionName = "SecretsVault";
    public required Dictionary<string, string> Secrets { get; init; } = new();
}

public interface IVaultService
{
    Task<string?> GetSecretAsync(string secretPath);
}