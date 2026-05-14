using System;
using System.IO;

namespace Inso.Els.Tests.Helpers
{
    /// <summary>Creates a unique temp directory for a test and cleans it up on dispose.</summary>
    public sealed class TempBufferDir : IDisposable
    {
        public string Path { get; }

        public TempBufferDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "els-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch { /* test teardown is best-effort */ }
        }
    }
}
