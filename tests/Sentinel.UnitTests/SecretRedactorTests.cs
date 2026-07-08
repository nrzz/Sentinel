using Sentinel.Infrastructure.AI;

namespace Sentinel.UnitTests.AI;

public class SecretRedactorTests
{
    private readonly SecretRedactor _redactor = new();

    [Fact]
    public void Redact_EmptyInput_ReturnsUnchanged()
    {
        var (text, wasRedacted) = _redactor.Redact("");

        Assert.Equal("", text);
        Assert.False(wasRedacted);
    }

    [Fact]
    public void Redact_PlainText_ReturnsUnchanged()
    {
        const string input = "This is a normal log message with no secrets.";

        var (text, wasRedacted) = _redactor.Redact(input);

        Assert.Equal(input, text);
        Assert.False(wasRedacted);
    }

    [Theory]
    [InlineData("api_key=sk-abc123secret")]
    [InlineData("password: mysecretpass")]
    [InlineData("token = bearer-token-value")]
    [InlineData("Authorization: abc123xyz")]
    public void Redact_CredentialPatterns_RedactsValue(string input)
    {
        var (text, wasRedacted) = _redactor.Redact(input);

        Assert.True(wasRedacted);
        Assert.Contains("[REDACTED]", text);
        Assert.DoesNotContain("abc123secret", text);
        Assert.DoesNotContain("mysecretpass", text);
    }

    [Fact]
    public void Redact_BearerToken_RedactsToken()
    {
        const string input = "Request failed with Bearer eyJhbGciOiJIUzI1NiJ9.payload.signature";

        var (text, wasRedacted) = _redactor.Redact(input);

        Assert.True(wasRedacted);
        Assert.Equal("Request failed with Bearer [REDACTED]", text);
    }

    [Fact]
    public void Redact_OpenAiKey_RedactsKey()
    {
        const string input = "Used key sk-abcdefghijklmnopqrstuvwxyz123456 for request";

        var (text, wasRedacted) = _redactor.Redact(input);

        Assert.True(wasRedacted);
        Assert.Contains("sk-[REDACTED]", text);
        Assert.DoesNotContain("abcdefghijklmnopqrstuvwxyz123456", text);
    }

    [Fact]
    public void Redact_PrivateKey_RedactsEntireKey()
    {
        const string input = """
            Config loaded:
            -----BEGIN RSA PRIVATE KEY-----
            MIIEpAIBAAKCAQEA...
            -----END RSA PRIVATE KEY-----
            Done.
            """;

        var (text, wasRedacted) = _redactor.Redact(input);

        Assert.True(wasRedacted);
        Assert.Contains("[REDACTED_PRIVATE_KEY]", text);
        Assert.DoesNotContain("BEGIN RSA PRIVATE KEY", text);
    }

    [Fact]
    public void Redact_MultipleSecrets_RedactsAll()
    {
        const string input = "api_key=secret123 and Bearer abc.def.ghi";

        var (text, wasRedacted) = _redactor.Redact(input);

        Assert.True(wasRedacted);
        Assert.DoesNotContain("secret123", text);
        Assert.DoesNotContain("abc.def.ghi", text);
    }
}
