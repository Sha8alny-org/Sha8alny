using System.Globalization;
using System.Net.Http.Json;
using System.Text;

namespace Sh8lny.Web.Logging;

public sealed class DiscordWebhookLogger : ILogger
{
    private const int MaxDiscordMessageLength = 1900;

    private readonly string _categoryName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _webhookUrl;

    public DiscordWebhookLogger(string categoryName, string? webhookUrl, IHttpClientFactory httpClientFactory)
    {
        _categoryName = categoryName;
        _webhookUrl = webhookUrl;
        _httpClientFactory = httpClientFactory;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return !string.IsNullOrWhiteSpace(_webhookUrl)
            && logLevel >= LogLevel.Information;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrWhiteSpace(message) && exception is null)
        {
            return;
        }

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

        var builder = new StringBuilder()
            .Append('[').Append(timestamp).Append("] [").Append(logLevel).AppendLine("]")
            .Append(_categoryName).Append(": ").AppendLine(message);

        if (exception is not null)
        {
            builder
                .Append("Exception: ")
                .Append(exception.GetType().Name)
                .Append(": ")
                .AppendLine(exception.Message);
        }

        var payload = new
        {
            content = $"```text\n{TrimForDiscord(builder.ToString())}\n```"
        };

        _ = SendAsync(payload);
    }

    private async Task SendAsync(object payload)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.PostAsJsonAsync(_webhookUrl!, payload);
        }
        catch
        {
        }
    }

    private static string TrimForDiscord(string value)
    {
        return value.Length <= MaxDiscordMessageLength
            ? value
            : value[..MaxDiscordMessageLength] + "...";
    }
}
