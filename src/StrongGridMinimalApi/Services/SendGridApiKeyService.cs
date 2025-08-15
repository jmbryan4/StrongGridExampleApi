namespace StrongGridMinimalApi.Services;

public sealed class SendGridApiKeyService(IVaultService vaultService) : ISendGridApiKeyService
{
    public async Task<string?> GetSendGridApiKeyAsync(string? subuser = null)
    {
        var secretPath = string.IsNullOrEmpty(subuser) ? "/sendgrid/default-api-key" : $"/sendgrid/{subuser.ToLowerInvariant()}-api-key";
        return await vaultService.GetSecretAsync(secretPath);
    }
}

public interface ISendGridApiKeyService
{
    Task<string?> GetSendGridApiKeyAsync(string? subuser = null);
}