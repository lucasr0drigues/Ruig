using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Application.Common.Dispatching
{
    public sealed class RuigDispatcher : IRuigDispatcher
    {
        // Cache of per-request-type strategies. Reflection is paid once per request
        // type per process; every subsequent call is a dictionary lookup + virtual call.
        private static readonly ConcurrentDictionary<Type, object> _strategies = new();

        private readonly IServiceProvider _serviceProvider;

        public RuigDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<TResponse> Send<TResponse>(IRuigRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var strategy = (DispatchStrategy<TResponse>)_strategies.GetOrAdd(
                request.GetType(),
                static requestType => BuildStrategy(requestType, typeof(TResponse)));

            return strategy.Dispatch(_serviceProvider, request, cancellationToken);
        }

        private static object BuildStrategy(Type requestType, Type responseType)
        {
            var strategyType = typeof(ConcreteDispatchStrategy<,>).MakeGenericType(requestType, responseType);
            return Activator.CreateInstance(strategyType)!;
        }

        private abstract class DispatchStrategy<TResponse>
        {
            public abstract Task<TResponse> Dispatch(IServiceProvider serviceProvider, object request, CancellationToken cancellationToken);
        }

        private sealed class ConcreteDispatchStrategy<TRequest, TResponse> : DispatchStrategy<TResponse>
            where TRequest : IRuigRequest<TResponse>
        {
            public override Task<TResponse> Dispatch(IServiceProvider serviceProvider, object request, CancellationToken cancellationToken)
            {
                var typedRequest = (TRequest)request;
                var handler = serviceProvider.GetRequiredService<IRuigRequestHandler<TRequest, TResponse>>();
                var behaviors = (IList<IRuigPipelineBehavior<TRequest, TResponse>>)
                    serviceProvider.GetServices<IRuigPipelineBehavior<TRequest, TResponse>>();

                if (behaviors.Count == 0)
                    return handler.Handle(typedRequest, cancellationToken);

                // Compose behaviors right-to-left so the first registered behavior runs
                // outermost.
                RuigPipelineDelegate<TResponse> pipeline = () => handler.Handle(typedRequest, cancellationToken);
                for (var i = behaviors.Count - 1; i >= 0; i--)
                {
                    var behavior = behaviors[i];
                    var next = pipeline;
                    pipeline = () => behavior.Handle(typedRequest, next, cancellationToken);
                }

                return pipeline();
            }
        }
    }
}
