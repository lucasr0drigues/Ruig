using FluentValidation;
using Ruig.Application.Common.Dispatching;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Application.Common.Behaviors
{
    public sealed class ValidationBehavior<TRequest, TResponse> : IRuigPipelineBehavior<TRequest, TResponse>
        where TRequest : IRuigRequest<TResponse>
    {
        private readonly IValidator<TRequest>[] _validators;

        public ValidationBehavior(System.Collections.Generic.IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators.ToArray();
        }

        public async Task<TResponse> Handle(TRequest request, RuigPipelineDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Length == 0)
                return await next();

            var context = new ValidationContext<TRequest>(request);

            var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = results
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);

            return await next();
        }
    }
}
