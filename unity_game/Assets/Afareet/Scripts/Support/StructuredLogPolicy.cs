using System.Text;

namespace Afareet.Support
{
    public enum LogSeverity { Trace, Debug, Info, Warning, Error, Critical }

    public static class StructuredLogPolicy
    {
        public static bool ShouldEmit(LogSeverity severity, LogSeverity minimum, bool releaseBuild)
        {
            if (releaseBuild && severity <= LogSeverity.Debug) return false;
            return severity >= minimum;
        }

        public static string NormalizeChannel(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel)) return "general";
            var input = channel.Trim().ToLowerInvariant();
            var builder = new StringBuilder();
            for (var i = 0; i < input.Length && builder.Length < 32; i++)
            {
                var c = input[i];
                builder.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' ? c : '_');
            }
            return builder.Length == 0 ? "general" : builder.ToString();
        }
    }
}
