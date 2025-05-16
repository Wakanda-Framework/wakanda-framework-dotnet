using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace org.wakanda.framework.Utilities
{
    /// <summary>
    /// JSON converter for Unix timestamp (seconds) properties
    /// </summary> 
    public class UnixTimestampJsonConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        if (long.TryParse(reader.GetString(), out var stringValue))
                            return stringValue;
                        break;
                        
                    case JsonTokenType.Number:
                        if (reader.TryGetInt64(out var numberValue))
                            return numberValue;
                        break;
                        
                    case JsonTokenType.Null:
                        return 0;
                }
            }
            catch (Exception ex)
            {
                throw new JsonException("Failed to parse Unix timestamp", ex);
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing Unix timestamp");
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    /// <summary>
    /// JSON converter for Unix timestamp (milliseconds) properties
    /// </summary>
    public class UnixTimestampMillisecondsJsonConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        if (long.TryParse(reader.GetString(), out var stringValue))
                            return stringValue;
                        break;
                        
                    case JsonTokenType.Number:
                        if (reader.TryGetInt64(out var numberValue))
                            return numberValue;
                        break;
                        
                    case JsonTokenType.Null:
                        return 0;
                }
            }
            catch (Exception ex)
            {
                throw new JsonException("Failed to parse Unix timestamp (milliseconds)", ex);
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing Unix timestamp (milliseconds)");
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    /// <summary>
    /// JSON converter that converts between DateTime and Unix timestamp (seconds)
    /// </summary>
    public class DateTimeUnixConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                long unixTime = 0;
                
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        if (long.TryParse(reader.GetString(), out var stringValue))
                            unixTime = stringValue;
                        break;
                        
                    case JsonTokenType.Number:
                        if (reader.TryGetInt64(out var numberValue))
                            unixTime = numberValue;
                        break;
                }

                return UnixTimeHelper.FromUnixTimeSeconds(unixTime);
            }
            catch (Exception ex)
            {
                throw new JsonException("Failed to parse DateTime from Unix timestamp", ex);
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.ToUnixTimeSeconds());
        }
    }
}