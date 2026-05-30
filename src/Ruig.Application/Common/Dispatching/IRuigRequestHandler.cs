using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Application.Common.Dispatching
{
    public interface IRuigRequestHandler<in TRequest, TResponse>
        where TRequest : IRuigRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }
}
