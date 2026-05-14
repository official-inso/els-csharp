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
        /// <param name="rethrow">
        /// When <c>true</c> (default), the original exception is re-thrown
        /// after being captured so other middleware can still handle the
        /// response. When <c>false</c>, the middleware writes a generic
        /// 500 response itself.
        /// </param>
        public static IApplicationBuilder UseElsExceptionHandling(this IApplicationBuilder app, bool rethrow = true)
        {
            if (app is null) throw new ArgumentNullException(nameof(app));

            var middleware = app.ApplicationServices.GetRequiredService<ElsExceptionMiddleware>();
            middleware.Rethrow = rethrow;
            return app.UseMiddleware<ElsExceptionMiddleware>();
        }
    }
}
