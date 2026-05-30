using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ruig.Application.Common.Behaviors;
using Ruig.Application.Common.Dispatching;

namespace Ruig.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            services.AddRuigDispatcher(assembly);

            services.AddValidatorsFromAssembly(assembly);

            services.AddTransient(
                typeof(IRuigPipelineBehavior<,>),
                typeof(ValidationBehavior<,>));

            return services;
        }
    }
}
