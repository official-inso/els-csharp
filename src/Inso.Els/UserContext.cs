using System.Collections.Generic;

namespace Inso.Els
{
    /// <summary>
    /// Information about the current user. When attached to an
    /// <see cref="IElsClient"/> via <c>client.User = ...</c>, the SDK
    /// enriches every captured entry with <c>user.id</c>, <c>user.email</c>,
    /// <c>user.name</c>, and each <see cref="Extra"/> key under <c>user.&lt;key&gt;</c>.
    /// </summary>
    public sealed class UserContext
    {
        /// <summary>User's unique identifier.</summary>
        public string? Id { get; init; }

        /// <summary>User's email address.</summary>
        public string? Email { get; init; }

        /// <summary>User's display name.</summary>
        public string? Name { get; init; }

        /// <summary>Additional user fields. Each pair is added to <c>Meta</c> as <c>user.&lt;key&gt;</c>.</summary>
        public IReadOnlyDictionary<string, string>? Extra { get; init; }
    }
}
