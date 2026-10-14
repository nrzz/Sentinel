using System.Text;

namespace Sentinel.Domain.Common;

public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-ID";
    public const string TenantHeaderName = "X-Tenant-ID";

    /// <summary>The longest caller-supplied correlation id that is accepted.</summary>
    public const int MaxLength = 64;

    public static string New() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// Returns <paramref name="candidate"/> when it is a well-formed correlation id, otherwise a freshly generated one.
    /// Well-formed means 1 to <see cref="MaxLength"/> characters drawn from ASCII letters, digits, '-', '_', '.' and ':',
    /// so a caller-supplied value can never carry line breaks or other control characters into a log entry.
    /// </summary>
    public static string Normalize(string? candidate)
    {
        if (string.IsNullOrEmpty(candidate) || candidate.Length > MaxLength)
        {
            return New();
        }

        var builder = new StringBuilder(candidate.Length);
        foreach (var c in candidate)
        {
            if (!IsAllowed(c))
            {
                return New();
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    private static bool IsAllowed(char c) =>
        c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_' or '.' or ':';
}
