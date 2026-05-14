using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Inso.Els.Tests.Helpers
{
    /// <summary>
    /// <see cref="HttpMessageHandler"/> that replays a queued sequence of
    /// responses. Captures every outgoing request so tests can assert on them.
    /// </summary>
    public sealed class StubHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses
            = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>();
        private readonly List<HttpRequestMessage> _requests = new List<HttpRequestMessage>();

        public IReadOnlyList<HttpRequestMessage> Requests => _requests;

        public StubHandler EnqueueOk(string body = "{\"created\":1}")
            => Enqueue(req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            });

        public StubHandler EnqueueStatus(HttpStatusCode status, string? body = null, Action<HttpResponseMessage>? configure = null)
            => Enqueue(req =>
            {
                var resp = new HttpResponseMessage(status);
                if (body is not null) resp.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                configure?.Invoke(resp);
                return resp;
            });

        public StubHandler EnqueueException(Exception exception)
            => Enqueue(_ => throw exception);

        public StubHandler Enqueue(Func<HttpRequestMessage, HttpResponseMessage> factory)
        {
            _responses.Enqueue(factory);
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(Clone(request));
            if (_responses.Count == 0)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"created\":1}", System.Text.Encoding.UTF8, "application/json"),
                });
            }
            return Task.FromResult(_responses.Dequeue().Invoke(request));
        }

        private static HttpRequestMessage Clone(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var h in request.Headers) clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
            if (request.Content is not null)
            {
                var bytes = request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                clone.Content = new ByteArrayContent(bytes);
                foreach (var h in request.Content.Headers) clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }
            return clone;
        }
    }
}
