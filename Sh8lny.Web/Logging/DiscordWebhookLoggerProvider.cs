namespace Sh8lny.Web.Logging;

public sealed class DiscordWebhookLoggerProvider : ILoggerProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _webhookUrl;

    public DiscordWebhookLoggerProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _webhookUrl = configuration["DiscordSettings:WebhookUrl"];
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new DiscordWebhookLogger(categoryName, _webhookUrl, _httpClientFactory);
    }

    public void Dispose()
    {
    }
}
