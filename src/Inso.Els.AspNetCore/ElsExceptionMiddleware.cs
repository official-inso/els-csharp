using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Inso.Els.AspNetCore
{
    /// <summary>
    /// Captures unhandled exceptions in the ASP.NET Core pipeline and reports
    /// them to ELS. When <see cref="Rethrow"/> is <c>true</c> (the default),
    /// the exception is re-thrown so upstream middleware can still handle the
    /// HTTP response. Otherwise the middleware returns a generic 500 response.
    /// </summary>
    internal sealed class ElsExceptionMiddleware : IMiddleware
    {
        private readonly IElsClient _client;

        public ElsExceptionMiddleware(IElsClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>Whether to re-throw the captured exception. Configured per <c>app.UseElsExceptionHandling</c> call.</summary>
        public bool Rethrow { get; set; } = true;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var options = new CaptureOptions
                {
                    Level = ElsLevel.Critical,
                }.WithHttpContext(context);

                _client.CaptureError(ex, options);

                if (Rethrow) throw;

                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync("Internal Server Error").ConfigureAwait(false);
                }
            }
        }
    }
}
