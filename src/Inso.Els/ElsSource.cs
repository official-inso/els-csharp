namespace Inso.Els
{
    /// <summary>Origin of an error entry.</summary>
    public enum ElsSource
    {
        /// <summary>Backend / server code.</summary>
        Server = 0,

        /// <summary>Frontend / browser / mobile.</summary>
        Client = 1,
    }

    /// <summary>Helpers for <see cref="ElsSource"/>.</summary>
    public static class ElsSourceExtensions
    {
        /// <summary>Returns the wire (JSON) string for the source.</summary>
        public static string ToWireValue(this ElsSource source)
        {
            return source == ElsSource.Client ? "client" : "server";
        }

        /// <summary>Parses a wire string into an <see cref="ElsSource"/>. Returns null on unknown input.</summary>
        public static ElsSource? Parse(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            switch (value!.Trim().ToLowerInvariant())
            {
                case "client": return ElsSource.Client;
                case "server": return ElsSource.Server;
                default: return null;
            }
        }
    }
}
