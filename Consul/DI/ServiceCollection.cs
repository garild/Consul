using System;
using System.Reflection;
using Consul.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Consul.DI
{
    public static class Extensions
    {
        private static readonly string ConsulSectionName = "consul";
        private static readonly string ServiceId = Guid.NewGuid().ToString("N");
        private static readonly string ServiceName = Assembly.GetCallingAssembly().GetName().Name;

        public static IServiceCollection AddConsul(this IServiceCollection services)
        {
            IConfiguration configuration;
            using (var serviceProvider = services.BuildServiceProvider())
            {
                configuration = serviceProvider.GetService<IConfiguration>();
            }

            var options = configuration.GetOptions<ConsulOptions>(ConsulSectionName);

            services.Configure<ConsulOptions>(configuration.GetSection(ConsulSectionName));

            services.AddTransient<IConsulServicesRegistry, ConsulServicesRegistry>();
            services.AddTransient<ConsulServiceDiscoveryMessageHandler>();
            services.AddHttpClient<IConsulHttpClient, ConsulHttpClient>()
                .AddHttpMessageHandler<ConsulServiceDiscoveryMessageHandler>();

            return services.AddSingleton<IConsulClient>(c => new ConsulClient(cfg =>
            {
                if (!string.IsNullOrEmpty(options.ServiceUrl))
                {
                    cfg.Address = new Uri(options.ServiceUrl);
                }
            }));
        }

        public static string UseConsul(this IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var consulOptions = scope.ServiceProvider.GetService<IOptions<ConsulOptions>>();
                var enabled = consulOptions.Value.Enabled;
                var address = consulOptions.Value.Address;
                var scheme = consulOptions.Value.Scheme;
                var uniqueId = ServiceId;
                var serviceName = ServiceName;
                var serviceId = $"{serviceName}:{uniqueId}";
               
                var port = consulOptions.Value.Port;
                var pingEndpoint = consulOptions.Value.PingEndpoint;
                var pingInterval = consulOptions.Value.PingInterval;
                var removeAfterInterval = consulOptions.Value.RemoveAfterInterval;

                var consulClient = scope.ServiceProvider.GetService<IConsulClient>();
                if (!enabled)
                {
                    return string.Empty;
                }
              
                if (string.IsNullOrWhiteSpace(address))
                {
                    throw new ArgumentException("Consul address can not be empty.",
                        nameof(consulOptions.Value.PingEndpoint));
                }
              
                var registration = new AgentServiceRegistration
                {
                    Name = serviceName,
                    ID = serviceId,
                    Address = address,
                    Port = port,
                    Tags = null
                };

                if (consulOptions.Value.PingEnabled)
                {
                    var check = new AgentServiceCheck
                    {
                        Interval = TimeSpan.FromSeconds(pingInterval),
                        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(removeAfterInterval),
                        HTTP = $"{scheme}://{address}:{port}{pingEndpoint}"
                    };
                    registration.Checks = new[] { check };
                }

                consulClient.Agent.ServiceRegister(registration);

                return serviceId;
            }
        }

        public static TModel GetOptions<TModel>(this IConfiguration configuration, string section) where TModel : new()
        {
            var model = new TModel();
            configuration.GetSection(section).Bind(model);

            return model;
        }
    }
}


