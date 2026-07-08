namespace Sentinel.Domain.Configuration;

public sealed class SentinelOptions
{
    public const string SectionName = "Sentinel";

    public string ServiceName { get; set; } = "sentinel";
    public string Environment { get; set; } = "development";
}

public sealed class PostgreSqlOptions
{
    public const string SectionName = "PostgreSQL";
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class ClickHouseOptions
{
    public const string SectionName = "ClickHouse";
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "sentinel";
    public string Audience { get; set; } = "sentinel-api";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

public sealed class AiOptions
{
    public const string SectionName = "AI";
    public string DefaultProvider { get; set; } = "ollama";
    public OllamaOptions Ollama { get; set; } = new();
    public OpenAiOptions OpenAi { get; set; } = new();
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.3;
}

public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
}

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}
