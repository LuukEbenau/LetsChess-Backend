using LetsChess_Backend.Logic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Ocelot.Configuration.File;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LetsChess_Backend
{
    //from: https://stackoverflow.com/questions/63421563/environment-variable-in-ocelot-config-file
    public class GlobalHosts : Dictionary<string, Uri> { }
    public static class OcelotFileExtensions
    {
        public static IServiceCollection AddOcelotPlaceholderSupport(
               this IServiceCollection services,
               IConfiguration configuration)
        {
            services.PostConfigure<FileConfiguration>(fileConfiguration =>
            {
                var globalHosts = configuration
                    .GetSection($"ServiceEndpoints")
                    .Get<GlobalHosts>();

                foreach (var route in fileConfiguration.Routes)
                {
                    ConfigureRoute(route, globalHosts);
                }
            });

            return services;
        }

        private static void ConfigureRoute(FileRoute route, GlobalHosts globalHosts)
        {
            foreach (var hostAndPort in route.DownstreamHostAndPorts)
            {
                var host = hostAndPort.Host;
                if (host.StartsWith("{") && host.EndsWith("}"))
                {
                    var placeHolder = host.TrimStart('{').TrimEnd('}');
                    if (globalHosts.TryGetValue(placeHolder, out var uri))
                    {
                        route.DownstreamScheme = uri.Scheme;
                        hostAndPort.Host = uri.Host;
                        hostAndPort.Port = uri.Port;
                    }
                }
            }
        }
    }
}
