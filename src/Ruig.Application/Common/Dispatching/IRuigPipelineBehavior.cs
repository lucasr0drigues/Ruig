using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Application.Common.Dispatching
{
    public delegate Task<TResponse> RuigPipelineDelegate<TResponse>();

    public interface IRuigPipelineBehavior<in TRequest, TResponse>
        where TRequest : IRuigRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, RuigPipelineDelegate<TResponse> next, CancellationToken cancellationToken);
    }
}
