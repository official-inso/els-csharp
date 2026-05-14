using System;

namespace Inso.Els
{
    /// <summary>Raised when <see cref="ElsOptions"/> validation fails.</summary>
    public sealed class ElsConfigurationException : ElsException
    {
        /// <summary>Creates a configuration exception with a message.</summary>
        public ElsConfigurationException(string message) : base(message) { }

        /// <summary>Creates a configuration exception with a message and an inner cause.</summary>
        public ElsConfigurationException(string message, Exception inner) : base(message, inner) { }
    }
}
