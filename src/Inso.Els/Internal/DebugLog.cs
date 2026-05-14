using System;
using System.IO;

namespace Inso.Els.Internal
{
    internal sealed class DebugLog
    {
        private readonly bool _enabled;
        private readonly TextWriter _writer;

        public DebugLog(ElsOptions options)
        {
            _enabled = options.Debug;
            _writer = options.DebugWriter ?? Console.Error;
        }

        public void Write(string format, params object?[] args)
        {
            if (!_enabled) return;
            try
            {
                _writer.WriteLine("[els] " + (args.Length == 0 ? format : string.Format(format, args)));
            }
            catch
            {
                // swallow — debug logging must never throw
            }
        }
    }
}
