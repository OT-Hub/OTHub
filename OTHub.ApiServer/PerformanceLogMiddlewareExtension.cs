using System;
using Microsoft.AspNetCore.Builder;

namespace OTHub.APIServer
{
    public static class PerformanceLogMiddlewareExtension
    {
        public static IApplicationBuilder UsePerformanceLog(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            return app.UseMiddleware<PerformanceLogMiddleware>();
        }
    }
}