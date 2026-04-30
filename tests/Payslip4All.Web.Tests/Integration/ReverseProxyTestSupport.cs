using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace Payslip4All.Web.Tests.Integration;

internal static class ReverseProxyTestSupport
{
    internal static int GetUnusedLoopbackPort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    internal sealed class UpstreamProbe : IAsyncDisposable
    {
        private readonly WebApplication _app;

        private UpstreamProbe(WebApplication app, string baseUrl)
        {
            _app = app;
            BaseUrl = baseUrl;
        }

        internal string BaseUrl { get; }

        internal static async Task<UpstreamProbe> StartAsync(Action<WebApplication> configure)
        {
            var port = GetUnusedLoopbackPort();
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedHost |
                    ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            var app = builder.Build();
            app.UseForwardedHeaders();
            configure(app);
            await app.StartAsync();

            return new UpstreamProbe(app, $"http://127.0.0.1:{port}");
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
