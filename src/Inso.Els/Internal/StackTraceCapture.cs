using System;
using System.Diagnostics;
using System.Text;

namespace Inso.Els.Internal
{
    /// <summary>Captures stack traces and strips SDK-internal frames.</summary>
    internal static class StackTraceCapture
    {
        private const string SdkNamespacePrefix = "Inso.Els.";

        /// <summary>
        /// Returns the stack trace for the given exception, or — when the
        /// exception has none — the current call stack. SDK-internal frames
        /// are filtered out so the output starts where the caller actually is.
        /// </summary>
        public static string FromException(Exception exception)
        {
            if (exception is null) throw new ArgumentNullException(nameof(exception));
            var trace = exception.StackTrace;
            if (!string.IsNullOrEmpty(trace))
            {
                return TidyFrames(trace!);
            }
            return Current(framesToSkip: 1);
        }

        /// <summary>
        /// Returns the current call stack as a string. <paramref name="framesToSkip"/>
        /// skips the SDK frames in addition to this method itself.
        /// </summary>
        public static string Current(int framesToSkip)
        {
            try
            {
                var st = new StackTrace(framesToSkip + 1, fNeedFileInfo: true);
                var raw = st.ToString();
                return TidyFrames(raw);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string TidyFrames(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            var lines = raw.Split('\n');
            var sb = new StringBuilder(raw.Length);
            foreach (var line in lines)
            {
                var trimmed = line.TrimEnd('\r');
                if (trimmed.Length == 0) continue;
                if (IsInternalFrame(trimmed)) continue;
                sb.Append(trimmed).Append('\n');
            }
            return sb.ToString();
        }

        private static bool IsInternalFrame(string line)
        {
            // ".NET StackTrace" formats look like:
            //   at Inso.Els.Internal.SomeType.SomeMethod() in /path/file.cs:line 42
            //   at Inso.Els.ElsClient.CaptureError(...)
            var idx = line.IndexOf("at ", StringComparison.Ordinal);
            if (idx < 0) return false;
            var after = line.Substring(idx + 3);
            if (after.StartsWith(SdkNamespacePrefix + "Internal.", StringComparison.Ordinal)) return true;
            if (after.StartsWith(SdkNamespacePrefix + "ElsClient.", StringComparison.Ordinal)) return true;
            if (after.StartsWith(SdkNamespacePrefix + "Sdk.", StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
