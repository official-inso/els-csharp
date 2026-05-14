using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Inso.Els.AspNetCore
{
    /// <summary>Extensions for registering the ELS exception middleware.</summary>
    public static class ElsApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the ELS exception-capturing middleware to the pipeline.
        /// Should be registered as early as possible (before any
        /// short-circuiting middleware) so exceptions thrown downstream are
        /// observed.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="configure">
        /// Optional configuration: set <see cref="ElsExceptionHandlerOptions.Mode"/>
        /// to choose between rethrow (default) and self-handling, or attach an
        /// <see cref="ElsExceptionHandlerOptions.OnException"/> callback.
        /// </param>
        public static IApplicationBuilder UseElsExceptionHandling(
            this IApplicationBuilder app,
            Action<ElsExceptionHandlerOptions>? configure = null)
        {
            if (app is null) throw new ArgumentNullException(nameof(app));

            var options = new ElsExceptionHandlerOptions();
            configure?.Invoke(options);

            var middleware = app.ApplicationServices.GetRequiredService<ElsExceptionMiddleware>();
            middleware.Options = options;
            return app.UseMiddleware<ElsExceptionMiddleware>();
        }

        /// <summary>
        /// Convenience overload for the common case <see cref="ElsExceptionMode.CaptureAndRethrow"/>.
        /// </summary>
        public static IApplicationBuilder UseElsExceptionHandling(this IApplicationBuilder app)
            => app.UseElsExceptionHandling((Action<ElsExceptionHandlerOptions>?)null);
    }
}
