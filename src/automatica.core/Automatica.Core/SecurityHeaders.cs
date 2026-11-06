using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core
{
    internal static class SecurityHeaders
    {
        /// <summary>
        /// Browser hardening headers. The CSP does not restrict scripts and styles (the web ui and the plugins' visualizations
        /// rely on inline styles and dynamic content), it blocks plugins, foreign base urls and framing by other sites.
        /// Embedding the visualization in another site needs its origin in <c>server:frame_ancestors</c> (space separated).
        /// </summary>
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, IConfiguration configuration)
        {
            var frameAncestors = configuration["server:frame_ancestors"];
            frameAncestors = string.IsNullOrWhiteSpace(frameAncestors) ? "'self'" : "'self' " + frameAncestors.Trim();
            var csp = $"frame-ancestors {frameAncestors}; object-src 'none'; base-uri 'self'";
            var sameOriginFrames = frameAncestors == "'self'";

            return app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["Referrer-Policy"] = "same-origin";
                headers["Content-Security-Policy"] = csp;
                if (sameOriginFrames)
                {
                    headers["X-Frame-Options"] = "SAMEORIGIN";
                }

                if (context.Request.IsHttps)
                {
                    headers["Strict-Transport-Security"] = "max-age=31536000";
                }

                await next();
            });
        }
    }
}
