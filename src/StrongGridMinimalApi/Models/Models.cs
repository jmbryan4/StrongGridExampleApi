using System.Text.Json.Serialization;

namespace StrongGridMinimalApi.Models;

public sealed record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

public record SimpleEmail
{
    public required string From { get; init; }
    public string? FromName { get; init; }
    public required string To { get; init; }
    public string? ToName { get; init; }
    public required string Subject { get; init; }
    public string? HtmlContent { get; init; }
    public string? TextContent { get; init; }
}

public sealed record EmailSendResult(string? MessageId, string Source)
{ 
    public bool Success => !string.IsNullOrEmpty(MessageId);
}

[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(SimpleEmail))]
[JsonSerializable(typeof(EmailSendResult))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;