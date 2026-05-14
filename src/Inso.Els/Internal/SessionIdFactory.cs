using System;
using System.Security.Cryptography;

namespace Inso.Els.Internal
{
    /// <summary>Generates session identifiers compatible with the Go SDK (<c>els-&lt;hex&gt;</c>).</summary>
    internal static class SessionIdFactory
    {
        private const string Prefix = "els-";

        public static string New()
        {
            var bytes = new byte[16];
#if NET6_0_OR_GREATER
            RandomNumberGenerator.Fill(bytes);
#else
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
#endif
            return Prefix + ToHex(bytes);
        }

        private static string ToHex(byte[] bytes)
        {
            var chars = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                chars[i * 2] = HexChar(b >> 4);
                chars[i * 2 + 1] = HexChar(b & 0xF);
            }
            return new string(chars);
        }

        private static char HexChar(int v) => (char)(v < 10 ? '0' + v : 'a' + (v - 10));
    }
}
