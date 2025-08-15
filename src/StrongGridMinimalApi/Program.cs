using StrongGridMinimalApi;
using StrongGridMinimalApi.Models;
using StrongGridMinimalApi.Services;
using StrongGrid;
using StrongGrid.Utilities;

var builder = WebApplication.CreateSlimBuilder(args);

// add AppJsonSerializerContext as highest priority for JSON serialization for source-generated types
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

// add StrongGrid client with resilience handler
builder.Services.AddHttpClient("StrongGrid", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    // other client configuration
}).AddStandardResilienceHandler();
builder.Services.AddScoped<StrongGridClient>(sp =>
{
    // linked by the "StrongGrid" name from the AddHttpClient above
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("StrongGrid");
    var defaultApiKey = builder.Configuration["SendGrid:DefaultApiKey"];
    ArgumentException.ThrowIfNullOrEmpty(defaultApiKey, "A SendGrid API key must be provided for StrongGrid");
    var options = new StrongGridClientOptions { LogLevelFailedCalls = LogLevel.Error, LogLevelSuccessfulCalls = LogLevel.Debug };
    var logger = sp.GetRequiredService<ILogger<StrongGridClient>>();
    return new StrongGridClient(defaultApiKey, httpClient, options, logger);
});
// configure StrongGrid client options from appsettings.json (or other configuration sources)
builder.Services.Configure<SendGridClientOptions>(builder.Configuration.GetSection("SendGrid"));

// secure secrets vault service. stub implementation for demo purposes
builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddScoped<ISendGridApiKeyService, SendGridApiKeyService>();
builder.Services.Configure<SecretsVaultOptions>(builder.Configuration.GetSection(SecretsVaultOptions.SectionName));
// factory for creating StrongGrid clients with subuser support
builder.Services.AddScoped<IStrongGridClientFactory, StrongGridClientFactory>();
var app = builder.Build();

#region Todo default example

var today = DateOnly.FromDateTime(DateTime.Today);
var sampleTodos = new Todo[] { new(1, "Walk the dog", today), new(2, "Do the laundry", today.AddDays(1)), new(3, "Clean the car", today.AddDays(2)) };
var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", () => sampleTodos);
todosApi.MapGet("/{id:int}", (int id) => sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo ? Results.Ok(todo) : Results.NotFound());

#endregion

// email sending example endpoints using StrongGrid
var emailApi = app.MapGroup("/email");
// direct StrongGrid client usage
emailApi.MapPost("/send-test", async (SimpleEmail email, StrongGridClient strongGridClient) =>
{
    var from = new StrongGrid.Models.MailAddress(email.From, email.FromName ?? "StrongGrid Sender");
    var to = new StrongGrid.Models.MailAddress(email.To, email.ToName ?? "StrongGrid Test Recipient");
    var messageId = await strongGridClient.Mail.SendToSingleRecipientAsync(to, from, email.Subject, email.HtmlContent, email.TextContent);
    return Results.Ok(new EmailSendResult(messageId, "StrongGrid Direct"));
});

// example using StrongGridClientFactory to create clients for different subusers
emailApi.MapPost("/send-marketing", async (SimpleEmail email, IStrongGridClientFactory factory) =>
{
    var client = factory.CreateClient("Marketing");
    var from = new StrongGrid.Models.MailAddress(email.From, email.FromName ?? "Marketing Team");
    var to = new StrongGrid.Models.MailAddress(email.To, email.ToName ?? "Customer");
    var messageId = await client.Mail.SendToSingleRecipientAsync(to, from, email.Subject, email.HtmlContent, email.TextContent);
    return Results.Ok(new EmailSendResult(messageId, "Marketing Client"));
});

// example using factory to create clients for different subusers pulling SendGrid API key from a secrets vault
emailApi.MapPost("/send-transactional", async (SimpleEmail email, IStrongGridClientFactory factory) =>
{
    var client = await factory.CreateClientAsync("Transactional");
    var from = new StrongGrid.Models.MailAddress(email.From, email.FromName ?? "System");
    var to = new StrongGrid.Models.MailAddress(email.To, email.ToName ?? "User");
    var messageId = await client.Mail.SendToSingleRecipientAsync(to, from, email.Subject, email.HtmlContent, email.TextContent);
    return Results.Ok(new EmailSendResult(messageId, "Transactional Client"));
});

app.Run();