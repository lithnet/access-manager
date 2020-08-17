using Microsoft.AspNetCore.Builder;

namespace Lithnet.AccessManager.Service.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseFeaturePolicy(this IApplicationBuilder app)
        {
            return app.UseFeaturePolicy("geolocation 'none';midi 'none';notifications 'none';push 'none';sync-xhr 'none';microphone 'none';camera 'none';magnetometer 'none';gyroscope 'none';speaker 'self';vibrate 'none';fullscreen 'self';payment 'none';");
        }

        public static IApplicationBuilder UseFeaturePolicy(this IApplicationBuilder app, string policy)
        {
            return app.Use(async (context, next) =>
                {
                    if (!context.Response.Headers.ContainsKey("Feature-Policy"))
                    {
                        context.Response.Headers.Add("Feature-Policy", policy);
                    }
                    await next.Invoke();
                });
        }

        public static IApplicationBuilder UseContentSecurityPolicy(this IApplicationBuilder app)
        {
            return app.UseContentSecurityPolicy("default-src 'none'; script-src 'self'; connect-src 'self'; img-src 'self'; style-src 'self'; font-src 'self';");
        }

        public static IApplicationBuilder UseContentSecurityPolicy(this IApplicationBuilder app, string csp)
        {
            return app.Use(async (context, next) =>
             {
                 if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
                 {
                     context.Response.Headers.Add("Content-Security-Policy", csp);
                 }

                 await next.Invoke();
             });
        }

        public static IApplicationBuilder UseContentTypeOptions(this IApplicationBuilder app)
        {
            return app.UseContentTypeOptions("nosniff");
        }

        public static IApplicationBuilder UseContentTypeOptions(this IApplicationBuilder app, string options)
        {
            return app.Use(async (context, next) =>
            {
                if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
                {
                    context.Response.Headers.Add("X-Content-Type-Options", options);
                }

                await next.Invoke();
            });
        }

        public static IApplicationBuilder UseReferrerPolicy(this IApplicationBuilder app)
        {
            return app.UseReferrerPolicy("strict-origin-when-cross-origin");
        }

        public static IApplicationBuilder UseReferrerPolicy(this IApplicationBuilder app, string policy)
        {
            return app.Use(async (context, next) =>
            {
                if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
                {
                    context.Response.Headers.Add("Referrer-Policy", policy);
                }

                await next.Invoke();
            });
        }

    }
}
