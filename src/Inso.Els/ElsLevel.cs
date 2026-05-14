using System;

namespace Inso.Els
{
    /// <summary>Severity level of an error entry.</summary>
    public enum ElsLevel
    {
        /// <summary>Diagnostic detail. Lowest priority.</summary>
        Debug = 0,

        /// <summary>Significant event but not an error.</summary>
        Info = 1,

        /// <summary>Potential issue worth investigating.</summary>
        Warning = 2,

        /// <summary>Operation failed.</summary>
        Error = 3,

        /// <summary>System down or data loss. Highest priority — never sampled out.</summary>
        Critical = 4,
    }

    /// <summary>Helpers for <see cref="ElsLevel"/>.</summary>
    public static class ElsLevelExtensions
    {
        /// <summary>Returns the wire (JSON) string for the level (e.g. <c>"critical"</c>).</summary>
        public static string ToWireValue(this ElsLevel level)
        {
            switch (level)
            {
                case ElsLevel.Debug: return "debug";
                case ElsLevel.Info: return "info";
                case ElsLevel.Warning: return "warning";
                case ElsLevel.Error: return "error";
                case ElsLevel.Critical: return "critical";
                default: return "error";
            }
        }

        /// <summary>Parses a wire string into an <see cref="ElsLevel"/>. Returns null on unknown input.</summary>
        public static ElsLevel? Parse(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            switch (value!.Trim().ToLowerInvariant())
            {
                case "debug": return ElsLevel.Debug;
                case "info": return ElsLevel.Info;
                case "warning":
                case "warn": return ElsLevel.Warning;
                case "error": return ElsLevel.Error;
                case "critical":
                case "fatal": return ElsLevel.Critical;
                default: return null;
            }
        }
    }
}
