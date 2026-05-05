using Ruig.Application.Common.Interfaces;

namespace Ruig.Infrastructure.Common
{
    public sealed class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
