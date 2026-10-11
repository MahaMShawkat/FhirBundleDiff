using System;
using System.Collections.Generic;
using System.Text;

namespace FhirDiff.Ai.Tests.AiTestHelpers
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        public List<string> RequestBodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBodies.Add(request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken));
            return _response;
        }
        private readonly HttpResponseMessage _response;

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }
    }
}
