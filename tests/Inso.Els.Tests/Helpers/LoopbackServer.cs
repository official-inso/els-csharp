using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Inso.Els.Tests.Helpers
{
    /// <summary>
    /// Thin convenience wrapper around <see cref="WireMockServer"/> with
    /// canned scenarios for the ELS endpoints used in tests.
    /// </summary>
    public sealed class LoopbackServer : IDisposable
    {
        private readonly WireMockServer _server;

        public LoopbackServer()
        {
            _server = WireMockServer.Start();
        }

        public string BaseUrl => _server.Url ?? throw new InvalidOperationException("Server not started");

        public WireMockServer Raw => _server;

        public void StubBatchOk(int? created = null)
        {
            _server
                .Given(Request.Create().WithPath("/errors/batch").UsingPost())
                .RespondWith(Response.Create().WithStatusCode((int)HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(created is null ? "{\"created\":1}" : "{\"created\":" + created + "}"));
        }

        public void StubSingleOk()
        {
            _server
                .Given(Request.Create().WithPath("/errors").UsingPost())
                .RespondWith(Response.Create().WithStatusCode((int)HttpStatusCode.Created)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{\"id\":\"abc\",\"traceId\":\"def\"}"));
        }

        public void StubHealth(HttpStatusCode status = HttpStatusCode.OK)
        {
            _server
                .Given(Request.Create().WithPath("/health").UsingGet())
                .RespondWith(Response.Create().WithStatusCode((int)status)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(status == HttpStatusCode.OK
                        ? "{\"status\":\"ok\"}"
                        : "{\"status\":\"unhealthy\"}"));
        }

        public void Reset() => _server.Reset();

        public IReadOnlyList<WireMock.Logging.ILogEntry> Log
            => System.Linq.Enumerable.ToList(_server.LogEntries);

        public void Dispose() => _server.Dispose();
    }
}
