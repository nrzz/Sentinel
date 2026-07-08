using System.Text.Json;
using System.Text.Json.Serialization;
using Sentinel.Sdk;
using Sentinel.Sdk.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Sentinel.CLI;

internal static class OutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static void Write(object value, OutputFormat format)
    {
        var text = format switch
        {
            OutputFormat.Json => JsonSerializer.Serialize(value, JsonOptions),
            OutputFormat.Yaml => YamlSerializer.Serialize(value),
            _ => JsonSerializer.Serialize(value, JsonOptions)
        };

        Console.WriteLine(text);
    }

    public static void WriteError(string message, OutputFormat format)
    {
        Write(new { error = message }, format);
        Environment.ExitCode = 1;
    }
}

internal enum OutputFormat
{
    Json,
    Yaml
}
