using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Ruig.Application.Common.Behaviors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assembly));

            services.AddValidatorsFromAssembly(assembly);

            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(ValidationBehavior<,>));

            return services;
        }
    }
}
