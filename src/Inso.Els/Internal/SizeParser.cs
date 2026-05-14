using System;
using System.Globalization;

namespace Inso.Els.Internal
{
    /// <summary>
    /// Parses byte-size strings like <c>"100MB"</c>, <c>"50KB"</c>, <c>"1GB"</c>
    /// produced by ergonomic configuration files. Plain numbers are interpreted
    /// as bytes. Returns the parsed value in bytes.
    /// </summary>
    internal static class SizeParser
    {
        public static bool TryParse(string? input, out long bytes)
        {
            bytes = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;

            var trimmed = input!.Trim();

            // Plain number → bytes.
            if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var raw))
            {
                bytes = raw;
                return raw >= 0;
            }

            // Match suffixes in descending length to avoid "B" eating "KB" tail.
            (string Suffix, long Multiplier)[] units =
            {
                ("TB", 1L << 40),
                ("GB", 1L << 30),
                ("MB", 1L << 20),
                ("KB", 1L << 10),
                ("B", 1L),
            };

            foreach (var (suffix, multiplier) in units)
            {
                if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var head = trimmed.Substring(0, trimmed.Length - suffix.Length).TrimEnd();
                    if (double.TryParse(head, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= 0)
                    {
                        bytes = (long)(value * multiplier);
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }
    }
}
