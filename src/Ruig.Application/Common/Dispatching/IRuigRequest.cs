namespace Ruig.Application.Common.Dispatching
{
    // Marker interface for a request handled by IRuigDispatcher. Carries the
    // response type so call sites stay strongly typed without runtime casts.
    public interface IRuigRequest<out TResponse>
    {
    }
}
