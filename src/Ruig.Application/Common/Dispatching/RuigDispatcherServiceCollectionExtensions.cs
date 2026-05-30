using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Ruig.Application.Common.Dispatching
{
    public static class RuigDispatcherServiceCollectionExtensions
    {
        // Scans the assembly for every concrete IRuigRequestHandler<,> and registers
        // it transient. Pipeline behaviors are registered separately by the caller.
        public static IServiceCollection AddRuigDispatcher(this IServiceCollection services, Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(assembly);

            services.AddScoped<IRuigDispatcher, RuigDispatcher>();

            var handlerInterface = typeof(IRuigRequestHandler<,>);

            var registrations = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                    .Select(i => (Service: i, Implementation: t)));

            foreach (var (service, implementation) in registrations)
            {
                services.AddTransient(service, implementation);
            }

            return services;
        }
    }
}
