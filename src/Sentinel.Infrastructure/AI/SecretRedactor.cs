using System.Text.RegularExpressions;

namespace Sentinel.Infrastructure.AI;

public interface ISecretRedactor
{
    (string RedactedText, bool WasRedacted) Redact(string input);
}

public sealed partial class SecretRedactor : ISecretRedactor
{
    [GeneratedRegex(@"(?i)(api[_-]?key|secret|password|token|authorization)\s*[:=]\s*['""]?([^\s'"";,]+)", RegexOptions.Compiled)]
    private static partial Regex CredentialPattern();

    [GeneratedRegex(@"(?i)Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.Compiled)]
    private static partial Regex BearerTokenPattern();

    [GeneratedRegex(@"(?i)sk-[A-Za-z0-9]{20,}", RegexOptions.Compiled)]
    private static partial Regex OpenAiKeyPattern();

    [GeneratedRegex(@"(?i)-----BEGIN\s+(?:RSA\s+)?PRIVATE\s+KEY-----[\s\S]*?-----END\s+(?:RSA\s+)?PRIVATE\s+KEY-----", RegexOptions.Compiled)]
    private static partial Regex PrivateKeyPattern();

    public (string RedactedText, bool WasRedacted) Redact(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return (input, false);
        }

        var redacted = input;
        var wasRedacted = false;

        redacted = ReplaceIfChanged(redacted, CredentialPattern(), match =>
        {
            wasRedacted = true;
            return $"{match.Groups[1].Value}=[REDACTED]";
        });

        redacted = ReplaceIfChanged(redacted, BearerTokenPattern(), _ =>
        {
            wasRedacted = true;
            return "Bearer [REDACTED]";
        });

        redacted = ReplaceIfChanged(redacted, OpenAiKeyPattern(), _ =>
        {
            wasRedacted = true;
            return "sk-[REDACTED]";
        });

        redacted = ReplaceIfChanged(redacted, PrivateKeyPattern(), _ =>
        {
            wasRedacted = true;
            return "[REDACTED_PRIVATE_KEY]";
        });

        return (redacted, wasRedacted);
    }

    private static string ReplaceIfChanged(string input, Regex pattern, Func<Match, string> replacer) =>
        pattern.Replace(input, match => replacer(match));
}
