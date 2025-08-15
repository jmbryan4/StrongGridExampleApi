using Microsoft.Extensions.Options;
using StrongGrid.Utilities;
using StrongGridMinimalApi.Services;

namespace StrongGridMinimalApi;

public interface IStrongGridClientFactory
{
    StrongGridClient CreateClient(string? subuser = null);
    Task<StrongGridClient> CreateClientAsync(string? subuser = null);
}

public sealed class StrongGridClientFactory(
    IHttpClientFactory httpClientFactory,
    IOptions<SendGridClientOptions> options,
    ISendGridApiKeyService sendGridApiKeyService,
    ILogger<StrongGridClient> logger) : IStrongGridClientFactory
{
    private const string StrongGridHttpClientName = "StrongGrid"; // could make this configurable
    
    public StrongGridClient CreateClient(string? subuser = null)
    {
        var httpClient = httpClientFactory.CreateClient(StrongGridHttpClientName);
        var config = options.Value;
        var apiKey = string.IsNullOrEmpty(subuser) ? config.DefaultApiKey : config.ApiKeys.GetValueOrDefault(subuser) ?? config.DefaultApiKey;
        ArgumentException.ThrowIfNullOrEmpty(apiKey, $"API key not found for subuser: {subuser}");
        var strongGridOptions = new StrongGridClientOptions { LogLevelFailedCalls = LogLevel.Error, LogLevelSuccessfulCalls = LogLevel.Debug };
        return new StrongGridClient(apiKey, httpClient, strongGridOptions, logger);
    }
    
    // this one is async because it fetches the API key from a secure vault service
    public async Task<StrongGridClient> CreateClientAsync(string? subuser = null)
    {
        var httpClient = httpClientFactory.CreateClient(StrongGridHttpClientName);
        var apiKey = await sendGridApiKeyService.GetSendGridApiKeyAsync(subuser);
        ArgumentException.ThrowIfNullOrEmpty(apiKey, $"API key not found for subuser: {subuser}");
        var strongGridOptions = new StrongGridClientOptions { LogLevelFailedCalls = LogLevel.Error, LogLevelSuccessfulCalls = LogLevel.Debug };
        return new StrongGridClient(apiKey, httpClient, strongGridOptions, logger);
    }
}

public sealed record SendGridClientOptions
{
    public required string DefaultApiKey { get; init; }
    public required Dictionary<string, string> ApiKeys { get; init; } = new();
}
