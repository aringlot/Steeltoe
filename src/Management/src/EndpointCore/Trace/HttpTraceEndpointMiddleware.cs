﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Trace
{
    public class HttpTraceEndpointMiddleware : EndpointMiddleware<HttpTraceResult>
    {
        private readonly RequestDelegate _next;

        public HttpTraceEndpointMiddleware(RequestDelegate next, HttpTraceEndpoint endpoint, IManagementOptions mgmtOptions, ILogger<HttpTraceEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions, logger: logger)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if (_endpoint.ShouldInvoke(_mgmtOptions, _logger))
            {
                return HandleTraceRequestAsync(context);
            }

            return Task.CompletedTask;
        }

        protected internal Task HandleTraceRequestAsync(HttpContext context)
        {
            var serialInfo = HandleRequest();
            _logger?.LogDebug("Returning: {0}", serialInfo);

            context.HandleContentNegotiation(_logger);
            return context.Response.WriteAsync(serialInfo);
        }
    }
}
