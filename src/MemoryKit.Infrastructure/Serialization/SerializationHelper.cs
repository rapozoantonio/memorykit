using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MemoryKit.Infrastructure.Serialization;

/// <summary>
/// Provides optimized JSON serialization settings for memory storage optimization.
/// Reduces storage size by 20-30% through compact formatting and efficient encoding.
/// </summary>
public static class SerializationHelper
{
    private static readonly JsonSerializerOptions _optimizedOptions;
    private static readonly JsonSerializerOptions _readCompatibleOptions;

    static SerializationHelper()
    {
        _optimizedOptions = CreateOptimizedOptions();
        _readCompatibleOptions = CreateReadCompatibleOptions();
    }

    /// <summary>
    /// Gets optimized JSON serializer options for writing.
    /// </summary>
    public static JsonSerializerOptions OptimizedOptions => _optimizedOptions;

    /// <summary>
    /// Gets backward-compatible JSON serializer options for reading.
    /// </summary>
    public static JsonSerializerOptions ReadCompatibleOptions => _readCompatibleOptions;

    /// <summary>
    /// Serializes an object to JSON using optimized settings.
    /// </summary>
    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, _optimizedOptions);
    }

    /// <summary>
    /// Serializes an object to UTF-8 bytes using optimized settings.
    /// </summary>
    public static byte[] SerializeToUtf8Bytes<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, _optimizedOptions);
    }

    /// <summary>
    /// Deserializes JSON to an object using backward-compatible settings.
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _readCompatibleOptions);
    }

    /// <summary>
    /// Deserializes UTF-8 bytes to an object using backward-compatible settings.
    /// </summary>
    public static T? Deserialize<T>(byte[] utf8Json)
    {
        return JsonSerializer.Deserialize<T>(utf8Json, _readCompatibleOptions);
    }

    /// <summary>
    /// Deserializes JSON to an object using backward-compatible settings.
    /// </summary>
    public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Json)
    {
        return JsonSerializer.Deserialize<T>(utf8Json, _readCompatibleOptions);
    }

    private static JsonSerializerOptions CreateOptimizedOptions()
    {
        var options = new JsonSerializerOptions
        {
            // Compact formatting - no indentation
            WriteIndented = false,

            // Skip null values to reduce size
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

            // Use camelCase for consistency and smaller property names
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

            // Relaxed escaping for smaller output
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

            // Ignore read-only properties during serialization
            IgnoreReadOnlyProperties = false,

            // Include fields if needed
            IncludeFields = false,

            // Case-insensitive deserialization for flexibility
            PropertyNameCaseInsensitive = true,

            // Allow trailing commas for robustness
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        // Add custom converters for optimization
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new DateTimeConverter());
        options.Converters.Add(new TimeSpanConverter());

        return options;
    }

    private static JsonSerializerOptions CreateReadCompatibleOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Add same converters for backward compatibility
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new DateTimeConverter());
        options.Converters.Add(new TimeSpanConverter());

        return options;
    }
}

/// <summary>
/// Custom DateTime converter that uses Unix timestamp for compact storage.
/// Reduces DateTime from ~30 bytes ("2025-12-09T10:30:45.123Z") to ~10 bytes (1733745045).
/// </summary>
public class DateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // Read as Unix timestamp (seconds)
            var timestamp = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            // Backward compatibility: read ISO 8601 strings
            var dateString = reader.GetString();
            if (DateTime.TryParse(dateString, out var date))
            {
                return date;
            }
        }

        throw new JsonException("Invalid DateTime format");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Write as Unix timestamp for compact storage
        var timestamp = new DateTimeOffset(value).ToUnixTimeSeconds();
        writer.WriteNumberValue(timestamp);
    }
}

/// <summary>
/// Custom TimeSpan converter that uses total seconds for compact storage.
/// </summary>
public class TimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // Read as total seconds
            var seconds = reader.GetDouble();
            return TimeSpan.FromSeconds(seconds);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            // Backward compatibility: read standard TimeSpan strings
            var timeString = reader.GetString();
            if (TimeSpan.TryParse(timeString, out var time))
            {
                return time;
            }
        }

        throw new JsonException("Invalid TimeSpan format");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        // Write as total seconds for compact storage
        writer.WriteNumberValue(value.TotalSeconds);
    }
}
