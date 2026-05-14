using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Inso.Els.AspNetCore
{
    /// <summary>
    /// Captures unhandled exceptions in the ASP.NET Core pipeline and reports
    /// them to ELS. Behavior is controlled via <see cref="ElsExceptionHandlerOptions"/>
    /// configured by <c>app.UseElsExceptionHandling(...)</c>.
    /// </summary>
    internal sealed class ElsExceptionMiddleware : IMiddleware
    {
        private readonly IElsClient _client;

        public ElsExceptionMiddleware(IElsClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Active options. The <c>UseElsExceptionHandling</c> extension updates
        /// this before adding the middleware to the pipeline.
        /// </summary>
        public ElsExceptionHandlerOptions Options { get; set; } = new ElsExceptionHandlerOptions();

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var captureOptions = new CaptureOptions
                {
                    Level = Options.Level,
                }.WithHttpContext(context);

                _client.CaptureError(ex, captureOptions);

                if (Options.OnException is not null)
                {
                    try
                    {
                        await Options.OnException(ex, context).ConfigureAwait(false);
                    }
                    catch (Exception hookEx)
                    {
                        // Hook must not break the pipeline, but operators
                        // should still see the failure: capture it ourselves
                        // so it lands in the ELS dashboard.
                        _client.CaptureError(hookEx,
                            url: "Inso.Els.AspNetCore/OnException",
                            level: ElsLevel.Warning);
                    }
                }

                if (Options.Mode == ElsExceptionMode.CaptureAndRethrow) throw;

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
