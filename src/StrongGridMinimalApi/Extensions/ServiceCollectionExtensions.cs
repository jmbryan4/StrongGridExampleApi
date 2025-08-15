using StrongGrid.Utilities;

namespace StrongGridMinimalApi.Extensions;

public static class ServiceCollectionExtensions
{
    private static readonly StrongGridClientOptions _defaultOptions = new()
    {
        LogLevelSuccessfulCalls = LogLevel.Debug, 
        LogLevelFailedCalls = LogLevel.Error
    };

    // consumer of StrongGrid library might make an extension method to add StrongGridClient with resilience handler and configuration
    public static IServiceCollection AddStrongGridClient(this IServiceCollection services, IConfiguration configuration)
    {
        // example caller usage:
        var defaultApiKey = configuration["SendGrid:DefaultApiKey"];
        ArgumentException.ThrowIfNullOrEmpty(defaultApiKey, "A SendGrid API key must be provided for StrongGrid");
        services.AddStrongGrid(defaultApiKey).ConfigureHttpClient(client => // caller can configure the httpclient
        {
            client.BaseAddress = new Uri("https://api.sendgrid.com/v3");
            client.Timeout = Timeout.InfiniteTimeSpan; // let resilience handler manage timeouts
            client.DefaultRequestHeaders.UserAgent.ParseAdd("StrongGridMinimalApi/1.0");
        }).AddStandardResilienceHandler(); // add a resilience handler to the httpclient used by StrongGridClient
        return services;
    }

    // could add to StrongGrid library as an extension method
    public static IHttpClientBuilder AddStrongGrid(this IServiceCollection services, string apiKey, StrongGridClientOptions? options = null,
        string httpClientName = "StrongGrid")
    {
        // leave any httpclient configuration to the caller or set some defaults
        var httpClientBuilder = services.AddHttpClient(httpClientName);
        var strongGridOptions = options ?? _defaultOptions;

        // register StrongGridClient with DI
        services.AddScoped<StrongGridClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientName);
            var logger = sp.GetRequiredService<ILogger<StrongGridClient>>();
            return new StrongGridClient(apiKey, httpClient, strongGridOptions, logger);
        });
        return httpClientBuilder; // return the IHttpClientBuilder for further configuration if needed
    }
}