using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Application.Common.Dispatching
{
    public interface IRuigDispatcher
    {
        Task<TResponse> Send<TResponse>(IRuigRequest<TResponse> request, CancellationToken cancellationToken = default);
    }
}
