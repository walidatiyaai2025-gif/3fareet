using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Afareet.Progression
{
    public sealed class CareerSaveCodec
    {
        public const int MaxStoredStars = 9999;

        public string Encode(CareerProgress progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));
            if (progress.Version != CareerProgress.CurrentVersion)
                throw new ArgumentOutOfRangeException(nameof(progress), "Only the current Career progress version can be encoded.");
            if (progress.Stars > MaxStoredStars)
                throw new ArgumentOutOfRangeException(nameof(progress), $"Career save stars cannot exceed {MaxStoredStars}.");

            var builder = new StringBuilder(128);
            builder.Append("{\"version\":");
            builder.Append(CareerProgress.CurrentVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"stars\":");
            builder.Append(progress.Stars.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"completedNodeIds\":");
            AppendStringArray(builder, progress.CompletedNodeIds);
            builder.Append(",\"claimedRewardIds\":");
            AppendStringArray(builder, progress.ClaimedRewardIds);
            builder.Append('}');
            return builder.ToString();
        }

        public CareerProgress Decode(string source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var root = new JsonParser(source).ParseRootObject();
            int? version = ReadOptionalInt(root, "version");
            if (!version.HasValue || version.Value == 0)
            {
                return new CareerProgress(
                    CareerProgress.CurrentVersion,
                    ClampStoredStars(ReadOptionalInt(root, "totalStars") ?? 0),
                    ReadStringArray(root, "completed"),
                    Array.Empty<string>());
            }

            if (version.Value == CareerProgress.CurrentVersion)
            {
                return new CareerProgress(
                    CareerProgress.CurrentVersion,
                    ClampStoredStars(ReadOptionalInt(root, "stars") ?? 0),
                    ReadStringArray(root, "completedNodeIds"),
                    ReadStringArray(root, "claimedRewardIds"));
            }

            throw new FormatException($"Unsupported Career save version: {version.Value}.");
        }

        private static int ClampStoredStars(int stars)
        {
            if (stars < 0)
                return 0;
            return stars > MaxStoredStars ? MaxStoredStars : stars;
        }

        private static int? ReadOptionalInt(Dictionary<string, JsonValue> root, string key)
        {
            JsonValue value;
            if (!root.TryGetValue(key, out value) || value.Kind == JsonValueKind.Null)
                return null;
            if (value.Kind != JsonValueKind.Number)
                throw new FormatException($"Career save field '{key}' must be an integer.");

            int parsed;
            if (!int.TryParse(value.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                throw new FormatException($"Career save field '{key}' must be a 32-bit integer.");
            return parsed;
        }

        private static IReadOnlyList<string> ReadStringArray(Dictionary<string, JsonValue> root, string key)
        {
            JsonValue value;
            if (!root.TryGetValue(key, out value) || value.Kind == JsonValueKind.Null)
                return Array.Empty<string>();
            if (value.Kind != JsonValueKind.Array)
                throw new FormatException($"Career save field '{key}' must be an array.");

            var result = new List<string>();
            for (var index = 0; index < value.Items.Count; index++)
            {
                var item = value.Items[index];
                if (item.Kind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.Text))
                    result.Add(item.Text);
            }
            return result;
        }

        private static void AppendStringArray(StringBuilder builder, IReadOnlyList<string> values)
        {
            builder.Append('[');
            for (var index = 0; index < values.Count; index++)
            {
                if (index > 0)
                    builder.Append(',');
                AppendJsonString(builder, values[index]);
            }
            builder.Append(']');
        }

        private static void AppendJsonString(StringBuilder builder, string value)
        {
            builder.Append('"');
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 0x20)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(character);
                        }
                        break;
                }
            }
            builder.Append('"');
        }

        private enum JsonValueKind
        {
            Null = 0,
            Boolean = 1,
            Number = 2,
            String = 3,
            Array = 4,
            Object = 5
        }

        private sealed class JsonValue
        {
            public JsonValueKind Kind { get; }
            public string Text { get; }
            public IReadOnlyList<JsonValue> Items { get; }
            public Dictionary<string, JsonValue> Object { get; }

            private JsonValue(JsonValueKind kind, string text, IReadOnlyList<JsonValue> items, Dictionary<string, JsonValue> valueObject)
            {
                Kind = kind;
                Text = text;
                Items = items;
                Object = valueObject;
            }

            public static JsonValue Null() => new JsonValue(JsonValueKind.Null, null, null, null);
            public static JsonValue Boolean(string text) => new JsonValue(JsonValueKind.Boolean, text, null, null);
            public static JsonValue Number(string text) => new JsonValue(JsonValueKind.Number, text, null, null);
            public static JsonValue String(string text) => new JsonValue(JsonValueKind.String, text, null, null);
            public static JsonValue Array(IReadOnlyList<JsonValue> items) => new JsonValue(JsonValueKind.Array, null, items, null);
            public static JsonValue ObjectValue(Dictionary<string, JsonValue> valueObject) => new JsonValue(JsonValueKind.Object, null, null, valueObject);
        }

        private sealed class JsonParser
        {
            private const int MaxDepth = 32;
            private readonly string source;
            private int index;

            public JsonParser(string source)
            {
                this.source = source;
            }

            public Dictionary<string, JsonValue> ParseRootObject()
            {
                SkipWhitespace();
                var value = ParseValue(0);
                SkipWhitespace();
                if (index != source.Length)
                    throw Error("Unexpected trailing content.");
                if (value.Kind != JsonValueKind.Object)
                    throw Error("Career save root must be an object.");
                return value.Object;
            }

            private JsonValue ParseValue(int depth)
            {
                if (depth > MaxDepth)
                    throw Error("JSON nesting is too deep.");

                SkipWhitespace();
                if (index >= source.Length)
                    throw Error("Unexpected end of JSON.");

                switch (source[index])
                {
                    case '{': return JsonValue.ObjectValue(ParseObject(depth + 1));
                    case '[': return JsonValue.Array(ParseArray(depth + 1));
                    case '"': return JsonValue.String(ParseString());
                    case 't': ReadLiteral("true"); return JsonValue.Boolean("true");
                    case 'f': ReadLiteral("false"); return JsonValue.Boolean("false");
                    case 'n': ReadLiteral("null"); return JsonValue.Null();
                    default:
                        if (source[index] == '-' || IsDigit(source[index]))
                            return JsonValue.Number(ParseNumber());
                        throw Error("Unexpected JSON token.");
                }
            }

            private Dictionary<string, JsonValue> ParseObject(int depth)
            {
                Expect('{');
                SkipWhitespace();
                var result = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
                if (TryConsume('}'))
                    return result;

                while (true)
                {
                    SkipWhitespace();
                    if (index >= source.Length || source[index] != '"')
                        throw Error("Object property name must be a string.");
                    var key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    var value = ParseValue(depth);
                    if (result.ContainsKey(key))
                        throw Error($"Duplicate JSON property '{key}'.");
                    result.Add(key, value);
                    SkipWhitespace();
                    if (TryConsume('}'))
                        return result;
                    Expect(',');
                }
            }

            private IReadOnlyList<JsonValue> ParseArray(int depth)
            {
                Expect('[');
                SkipWhitespace();
                var result = new List<JsonValue>();
                if (TryConsume(']'))
                    return result;

                while (true)
                {
                    result.Add(ParseValue(depth));
                    SkipWhitespace();
                    if (TryConsume(']'))
                        return result;
                    Expect(',');
                }
            }

            private string ParseString()
            {
                Expect('"');
                var builder = new StringBuilder();
                while (index < source.Length)
                {
                    var character = source[index++];
                    if (character == '"')
                        return builder.ToString();
                    if (character < 0x20)
                        throw Error("Unescaped control character in JSON string.");
                    if (character != '\\')
                    {
                        builder.Append(character);
                        continue;
                    }

                    if (index >= source.Length)
                        throw Error("Incomplete JSON escape sequence.");
                    var escape = source[index++];
                    switch (escape)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u': builder.Append(ParseUnicodeEscape()); break;
                        default: throw Error("Unsupported JSON escape sequence.");
                    }
                }
                throw Error("Unterminated JSON string.");
            }

            private char ParseUnicodeEscape()
            {
                if (index + 4 > source.Length)
                    throw Error("Incomplete Unicode escape sequence.");

                var value = 0;
                for (var count = 0; count < 4; count++)
                {
                    var digit = HexValue(source[index++]);
                    if (digit < 0)
                        throw Error("Invalid Unicode escape sequence.");
                    value = (value << 4) | digit;
                }
                return (char)value;
            }

            private string ParseNumber()
            {
                var start = index;
                if (source[index] == '-')
                    index++;

                if (index >= source.Length)
                    throw Error("Incomplete JSON number.");
                if (source[index] == '0')
                {
                    index++;
                }
                else
                {
                    if (!IsDigitOneToNine(source[index]))
                        throw Error("Invalid JSON number.");
                    while (index < source.Length && IsDigit(source[index]))
                        index++;
                }

                if (index < source.Length && source[index] == '.')
                {
                    index++;
                    if (index >= source.Length || !IsDigit(source[index]))
                        throw Error("Invalid JSON number fraction.");
                    while (index < source.Length && IsDigit(source[index]))
                        index++;
                }

                if (index < source.Length && (source[index] == 'e' || source[index] == 'E'))
                {
                    index++;
                    if (index < source.Length && (source[index] == '+' || source[index] == '-'))
                        index++;
                    if (index >= source.Length || !IsDigit(source[index]))
                        throw Error("Invalid JSON number exponent.");
                    while (index < source.Length && IsDigit(source[index]))
                        index++;
                }

                return source.Substring(start, index - start);
            }

            private void ReadLiteral(string literal)
            {
                if (index + literal.Length > source.Length ||
                    string.CompareOrdinal(source, index, literal, 0, literal.Length) != 0)
                {
                    throw Error($"Invalid JSON literal; expected '{literal}'.");
                }
                index += literal.Length;
            }

            private void SkipWhitespace()
            {
                while (index < source.Length)
                {
                    var character = source[index];
                    if (character != ' ' && character != '\t' && character != '\r' && character != '\n')
                        return;
                    index++;
                }
            }

            private void Expect(char expected)
            {
                SkipWhitespace();
                if (index >= source.Length || source[index] != expected)
                    throw Error($"Expected '{expected}'.");
                index++;
            }

            private bool TryConsume(char expected)
            {
                SkipWhitespace();
                if (index >= source.Length || source[index] != expected)
                    return false;
                index++;
                return true;
            }

            private FormatException Error(string message)
            {
                return new FormatException($"{message} Position {index}.");
            }

            private static bool IsDigit(char character) => character >= '0' && character <= '9';
            private static bool IsDigitOneToNine(char character) => character >= '1' && character <= '9';

            private static int HexValue(char character)
            {
                if (character >= '0' && character <= '9') return character - '0';
                if (character >= 'a' && character <= 'f') return character - 'a' + 10;
                if (character >= 'A' && character <= 'F') return character - 'A' + 10;
                return -1;
            }
        }
    }
}
