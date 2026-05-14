using System;

namespace Inso.Els
{
    /// <summary>Base type for all ELS SDK exceptions.</summary>
    public class ElsException : Exception
    {
        /// <summary>Initializes the exception with a message.</summary>
        public ElsException(string message) : base(message) { }

        /// <summary>Initializes the exception with a message and an inner cause.</summary>
        public ElsException(string message, Exception inner) : base(message, inner) { }
    }
}
