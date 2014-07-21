﻿using System;
using System.Net;
using System.Threading.Tasks;
using Metrics;
using Metrics.Reporters;
using Metrics.Visualization;
using Microsoft.Owin;

namespace Owin.Metrics
{
    public static class MetricsOwinAppExtensions
    {
        public static IAppBuilder UseMetrics(this IAppBuilder app, Action<MetricsConfig> metricsConfigCallback,
            Action<OwinMetricsConfig> owinMetricsConfigCallback)
        {
            var config = Metric.Config;

            metricsConfigCallback(config);

            app.Use((context, next) =>
            {
                if (context.Request.Path.Value.EndsWith("/metrics"))
                {
                    return GetFlotWebApp(context.Response);
                }

                if (context.Request.Path.Value.EndsWith("/json"))
                {
                    return GetJsonContent(context.Response, config);
                }

                if (context.Request.Path.Value.EndsWith("/health"))
                {
                    return GetHealthStatus(context.Response, config);
                }

                if (context.Request.Path.Value.EndsWith("/text"))
                {
                    return GetAsHumanReadable(context.Response, config);
                }

                if (context.Request.Path.Value.EndsWith("/ping"))
                {
                    return GetPingContent(context.Response);
                }

                return next();
            });

            owinMetricsConfigCallback(new OwinMetricsConfig(app, config.Registry));

            return app;
        }

        private static async Task GetAsHumanReadable(IOwinResponse owinResponse, MetricsConfig config)
        {
            var report = new StringReporter();
            report.RunReport(config.Registry, config.HealthStatus);
            owinResponse.ContentType = "text/plain";
            await owinResponse.WriteAsync(report.Result);
        }

        private static async Task GetFlotWebApp(IOwinResponse owinResponse)
        {
            owinResponse.ContentType = "text/html";
            await owinResponse.WriteAsync(FlotWebApp.GetFlotApp());
        }

        private static async Task GetPingContent(IOwinResponse owinResponse)
        {
            owinResponse.ContentType = "text/plain";
            await owinResponse.WriteAsync("pong");
        }

        private static async Task GetJsonContent(IOwinResponse owinResponse, MetricsConfig config)
        {
            var content = RegistrySerializer.GetAsJson(config.Registry);
            owinResponse.ContentType = "text/json";
            await owinResponse.WriteAsync(content);
        }

        private static async Task GetHealthStatus(IOwinResponse owinResponse, MetricsConfig config)
        {
            var status = config.HealthStatus();
            var content = HealthCheckSerializer.Serialize(status);
            owinResponse.ContentType = "application/json";
            owinResponse.StatusCode = (int)(status.IsHealty ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
            await owinResponse.WriteAsync(content);
        }
    }
}